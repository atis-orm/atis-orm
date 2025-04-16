using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Exceptions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating converters that handle MemberExpression instances.
    ///     </para>
    /// </summary>
    public class MemberExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public MemberExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression memberExpression)
            {
                converter = new MemberExpressionConverter(this.Context, memberExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for handling MemberExpression instances and converting them to SQL expressions.
    ///     </para>
    /// </summary>
    public class MemberExpressionConverter : LinqToSqlExpressionConverterBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The MemberExpression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public MemberExpressionConverter(IConversionContext context, MemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether this instance is a leaf node in the <c>MemberExpression</c> chain.
        ///     </para>
        /// </summary>
        protected virtual bool IsLeafNode => this.ParentConverter?.GetType() != this.GetType();

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var parent = convertedChildren[0];
            var path = this.Expression.GetModelPath();

            if (parent is ISqlReferenceExpression refExpression)
                parent = refExpression.Reference;

            SqlExpression[] result;
            SqlExpression resultSource;

            if (parent is SqlQueryExpression sqlQuery)          // x is a query
            {
                // either we'll be returning the columns that are matched with the path
                // or matched data sources

                if (sqlQuery.Projection != null)        // if projection has been applied
                {
                    // we'll match with the projection
                    // if the projection has been applied, then we'll match with full or partial
                    // we cannot go anywhere else if the projection has been applied in the query
                    var columnExpressions = this.MatchWithProjection(path.Last(), sqlQuery);
                    if (!this.TryHandleSubQueryProjection(columnExpressions, out var newResult))
                        result = columnExpressions;
                    else
                        result = newResult;
                    resultSource = sqlQuery;
                }
                else
                {
                    var matchedDataSources = sqlQuery.AllQuerySources.Where(x => x.ModelPath.StartsWith(path)).ToArray();
                    var dataSourceReferences = matchedDataSources.Select(x => this.SqlFactory.CreateDataSourceReference(x)).ToArray();
                    result = dataSourceReferences;
                    resultSource = sqlQuery;
                    // x.Field, in-case of if x is a SqlQueryExpression and there is only 1 data source available
                    // above result will have no items, so we need to check if there is any column available
                    if (result.Length == 0)
                    {
                        var dsToUse = sqlQuery.HandleCteOrUsualDataSource(sqlQuery.DefaultDataSource ?? sqlQuery.InitialDataSource);
                        result = this.GetMatchingColumnsFromDataSource(path.Last(), dsToUse);
                        resultSource = dsToUse;
                    }
                }
            }
            else if (parent is SqlCollectionExpression sqlDsCollection && sqlDsCollection.SqlExpressions.Any(x => x is SqlDataSourceReferenceExpression))
            {
                var dsCollection = sqlDsCollection.SqlExpressions.Cast<SqlDataSourceReferenceExpression>()
                                                    .Select(x => x.Reference)
                                                    .ToArray();
                if (dsCollection.Length > 1)
                {
                    // it means we have to match in the data sources because there are multiple data sources
                    result = dsCollection.Where(x => x.ModelPath.StartsWith(path))
                                .Select(x => this.SqlFactory.CreateDataSourceReference(x))
                                .ToArray();
                    resultSource = parent;
                }
                else
                {
                    result = GetMatchingColumnsFromDataSource(path.Last(), dsCollection.First());
                    if (result.Length == 0)
                        // this is a possibility that the collection is nested with only 1 collection
                        //      x.dataSourceCollection.DataSource1.Field
                        // so in above case, x.dataSourceCollection will return Collection of SqlDataSourceReferenceExpression
                        // and it will only have 1 SqlDataSourceReferenceExpression, but we cannot return sql columns because
                        // it has another data source nested i.e. DataSource1
                        result = sqlDsCollection.SqlExpressions.Cast<SqlDataSourceReferenceExpression>().ToArray();
                    resultSource = dsCollection.First();
                }
            }
            else if (parent is SqlCollectionExpression sqlColCollection && sqlColCollection.SqlExpressions.Any(x => x is SqlColumnExpression))
            {
                var sqlColumns = sqlColCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
                if (sqlColumns.Length > 1)
                {
                    var p = new ModelPath(path);
                    /*
                     * .Select(x => new { FullDetail = new { Employee = x.o.e, Name = x.o.e.Name }, x.o.ed.RowId, x.m })
                     * .Select(x => new { x.FullDetail.Employee.RowId, x.FullDetail.Employee.Name, EmployeeDegreeRowId = x.RowId, ManagerName = x.m.Name })
                     * 
                     * 2nd select is selecting a nested column, therefore, first projection needs to create a model path like this
                     *      FullDetail.Employee.EmployeeId, FullDetail.Employee.Name, etc.
                     *      FullDetail.Name
                     *      RowId
                     *      m.EmployeeId, m.Name, etc.
                     *      
                     * Suppose we are converting first item in 2nd Select, `x.FullDetail.Employee.RowID`, 
                     *      x => Query
                     *      x.FullDetail => there will projection in the query so it will get all the columns which are
                     *                          starting with FullDetail and it will create a *collection* SqlColumnExpression
                     *      x.FullDetail.Employee => here we'll land here and we need to match all the columns that are starting
                     *                                  with ModelPath FullDetail.Employee
                     */
                    result = sqlColumns.Where(x => x.ModelPath.Equals(p) || x.ModelPath.StartsWith(path)).ToArray();
                    resultSource = parent;
                }
                else
                {
                    // we should never land here
                    throw new InvalidOperationException($"Should never land here");
                }
            }
            else if (parent is SqlDataSourceExpression parentDs)
            {
                result = GetMatchingColumnsFromDataSource(path.Last(), parentDs);
                resultSource = parentDs;
            }
            else
                throw new InvalidOperationException($"Unknown case");

            if (result.Length == 0)
                throw new UnresolvedMemberAccessException(this.Expression.ToString(), parent.NodeType);

            if (!result.Any(x => (x is SqlDataSourceReferenceExpression) ||
                                (x is SqlColumnExpression)))
                throw new InvalidOperationException($"result does not contain any {nameof(SqlDataSourceReferenceExpression)} or {nameof(SqlColumnExpression)}");

            if (this.IsLeafNode)
            {
                if (result.Length > 1)
                {
                    if (result.All(x => x is SqlColumnExpression))
                    {
                        result = result.Cast<SqlColumnExpression>()
                                        .Select(x => this.CreateSqlColumn(x.ColumnExpression, x.ColumnAlias, x.ModelPath.RemovePrefixPath(path)))
                                        .ToArray();
                    }
                    else if (result.All(x => x is SqlDataSourceReferenceExpression))
                    {
                        // we'll usually reach here in case if the data source is directly selected in projection
                        // e.g. q.Select(x => x.nestedShape)        <- nestedShape = { t1, t2 }
                        //      so before this selection, we had to select a field like this, x.nestedShape.t1.Field1
                        //      but after this selection we'll select field like this, x.t1.Field1
                        var newResult = new List<SqlExpression>();
                        var resultAsDataSourceRef = result.Cast<SqlDataSourceReferenceExpression>().ToArray();
                        for (var i = 0; i < resultAsDataSourceRef.Length; i++)
                        {
                            var ds = resultAsDataSourceRef[i].Reference;
                            // from above example, path would be t1, so we'll create a new path using data source's path
                            // like this,
                            //      data source path = nestedShape.t1
                            //      given path = t1     =>  remove until t1
                            //      new path would be = t1
                            var newModelPath = ds.ModelPath.RemovePrefixPath(path);
                            newResult.Add(this.CreateSqlColumn(this.SqlFactory.CreateDataSourceReference(ds), null, newModelPath));
                        }
                        result = newResult.ToArray();
                    }
                    else
                        throw new InvalidOperationException($"result.Length > 1 and collection type is neither SqlColumnExpression nor SqlDataSourceReferenceExpression.");

                    // This is usually not possible that a MemberExpression at leaf node level will return a collection,
                    // however, this is possible in-case if a the MemberExpression is a custom object.
                    // For example,
                    //      var q = db.Table1
                    //                  .LeftJoin(db.Table2, (t1, t2) => new { t1, t2 }, n => n.t1.PK == n.t2.FK)
                    //                  .LeftJoin(db.Table3, (oldType, joinedType) => new { o = oldType, joinedType = t3 }, n => n.t3.PK == n.o.t1.PK)
                    //                  .Select(x => x.o);
                    // In above example, x.o is a collection of SqlDataSourceReferenceExpression, and we are at leaf node level.
                    // Another case could be
                    //      ... a query ...
                    //          .Select(x => new { OuterProperty = new { InnerProperty = new { x.Field1, x.Field2 } } })
                    //          .Select(x => x.OuterProperty.InnerProperty );
                    // In above example, x.OuterProperty.InnerProperty is a collection of SqlColumnExpression, and we are at leaf node level.
                    return this.SqlFactory.CreateSelectedCollection(resultSource, result);
                }

                var firstItem = result.First();
                if (firstItem is SqlColumnExpression sqlColExpr)
                    return sqlColExpr.ColumnExpression;
                else
                {
                    // it means firstItem will be SqlDataSourceReferenceExpression
                    var resultAsDsRef = firstItem as SqlDataSourceReferenceExpression
                                            ??
                                            throw new InvalidOperationException($"Expected {nameof(SqlDataSourceReferenceExpression)} but got {firstItem.GetType().Name}");
                    var resultDs = resultAsDsRef.Reference;
                    if (resultDs is SqlDataSourceExpression sqlQueryDs)
                    {
                        if (sqlQueryDs.QuerySource is SqlQueryExpression innerQuery &&
                            innerQuery.Projection.TryGetScalarColumn(out var scalarColExpr))
                            return this.CreateSqlDataSourceColumn(sqlQueryDs, scalarColExpr.ColumnAlias);
                        else if (sqlQueryDs.NodeType == SqlExpressionType.SubQueryDataSource)
                        {
                            // this in-case if other data source directly selected
                            var otherDataSourceSqlQuery = sqlQueryDs.QuerySource as SqlQueryExpression
                                                            ??
                                                            throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} but got {sqlQueryDs.QuerySource.GetType().Name}");
                            var newSqlQuery = otherDataSourceSqlQuery.CreateCopy();
                            return newSqlQuery;
                        }
                         
                    }

                    return firstItem;
                }
            }
            else
                return this.SqlFactory.CreateCollection(result);
        }

        private SqlDataSourceColumnExpression CreateSqlDataSourceColumn(SqlDataSourceExpression dataSourceExpression, string alias)
        {
            return this.SqlFactory.CreateDataSourceColumn(dataSourceExpression, alias);
        }

        private SqlColumnExpression[] GetMatchingColumnsFromDataSource(string lastPathSegment, SqlDataSourceExpression ds)
        {
            SqlColumnExpression[] result;
            if (ds.QuerySource is SqlTableExpression table)
            {
                var tableColumns = table.TableColumns.Where(x => x.ModelPropertyName == lastPathSegment).ToArray();
                var dataSourceColumns = tableColumns.Select(x => new { x.ModelPropertyName, DsColumn = this.CreateSqlDataSourceColumn(ds, x.DatabaseColumnName) });
                var columns = dataSourceColumns.Select(x => this.CreateSqlColumn(x.DsColumn, x.ModelPropertyName, ds.ModelPath.Append(x.ModelPropertyName))).ToArray();
                result = columns;
            }
            else if (ds.QuerySource is SqlQueryExpression subQuery)
            {
                SqlQueryExpression queryToUse;
                if (subQuery.Projection != null)
                {
                    queryToUse = subQuery;
                }
                else
                {
                    queryToUse = subQuery.InitialDataSource.QuerySource as SqlQueryExpression
                                        ??
                                        throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} but got {subQuery.InitialDataSource.QuerySource?.GetType().Name}.");
                }

                var matchedProjections = this.MatchWithProjection(lastPathSegment, queryToUse);
                if (!this.TryHandleSubQueryProjection(matchedProjections, out result))
                {
                    /*
                     var q = QueryExtensions
                            .From(queryProvider, () => new
                            {
                                q1 = QueryExtensions.From(queryProvider, () => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                                                            .LeftJoin(f1 => f1.ed, fj1 => fj1.e.EmployeeId == fj1.ed.EmployeeId)
                                                            .Schema(),
                                m = QueryExtensions.Table<Employee>()
                            })
                            .LeftJoin(f2 => f2.m, fj2 => fj2.q1.e.ManagerId == fj2.m.EmployeeId)
                            .LeftJoin(new Queryable<Employee>(queryProvider), (o, j3) => new { o, m2 = j3 }, n => n.o.q1.e.ManagerId == n.m2.EmployeeId)
                            .Select(x => new { EmRowId = x.o.q1.e.RowId, EdRowId = x.o.q1.ed.RowId, M1RowId = x.o.m.RowId, M2RowId = x.m2.RowId })
                            ;
                    
                     *  when converting above query, the `q1` is going to be translated as a sub-query and will be added to query as data source
                     *  with ModelPath = q1. Internal in this sub-query the projection has been applied,
                     *      [0] = e.EmployeeId, [1] = e.EmployeeName, [2] = ed.EmployeeId, [3] = ed.DegreeName, etc.
                     *  
                     *  when we are going to select the sub-query property in outer query, e.g. fj2.q1.e.ManagerId, it will converted like this
                     *      fj2                 =>  Query (outer)
                     *      fj2.q1              =>  Inner Sub-Query (a data source within the outer Query)
                     *         ** we'll be here in this method for fj2.q1.e **
                     *      fj2.q1.e            =>  Now parent is inner query which has been closed and projection has been applied
                     *                              so here we'll find all the projections under q1 that are starting with 'e' and we'll
                     *                              find [0] e.EmployeeId and [1] e.EmployeeName as shown above.
                     *                              Now we need to return this projection to next level that is fj2.q1.e.ManagerId,
                     *                              if we simply return as is, the path of returned SqlColumnExpression will be e.ManagerId but
                     *                              next level MemberExpression path is q1.e.ManagerId which will NOT match, 
                     *                              that's why below we are adding in start so that e.ManagerId will become q1.e.ManagerId
                     *          ** for below MemberExpression we'll not land here **
                     *      fj2.q1.e.ManagerId =>   will be handled outside this method in main converter
                     */

                    var subQueryColumnAliases = this.MatchWithProjection(lastPathSegment, queryToUse)
                                                        .Select(x => new { DsModel = ds.ModelPath.Append(x.ModelPath), ColExpr = this.CreateSqlDataSourceColumn(ds, x.ColumnAlias) })
                                                        .ToArray();
                    var columns = subQueryColumnAliases.Select(x => this.CreateSqlColumn(x.ColExpr, x.ColExpr.ColumnName, x.DsModel)).ToArray();
                    result = columns;
                }
            }
            else
                throw new InvalidOperationException($"firstDataSource is neither Table nor a Query");
            return result;
        }

        private SqlColumnExpression[] MatchWithProjection(string lastPathSegment, SqlQueryExpression sqlQuery)
        {
            if (sqlQuery.Projection == null)
                throw new InvalidOperationException("Projection is null");

            var collection = sqlQuery.Projection as SqlCollectionExpression
                                ??
                                throw new InvalidOperationException($"Expected {nameof(SqlCollectionExpression)} but got {sqlQuery.Projection.GetType().Name}");
            return collection
                        .SqlExpressions
                        .Cast<SqlColumnExpression>()
                        .Where(x => x.ModelPath.StartsWith(new[] { lastPathSegment }))
                        .ToArray();
        }

        private bool TryHandleSubQueryProjection(SqlColumnExpression[] matchedProjections, out SqlColumnExpression[] updatedResult)
        {
            if (matchedProjections.Length == 1 &&
                matchedProjections[0].ColumnExpression is SqlQueryExpression singleQuery)
            {
                var copied = singleQuery.CreateCopy();
                var projection = this.CreateSqlColumn(copied, matchedProjections[0].ColumnAlias, matchedProjections[0].ModelPath);
                updatedResult = new[] { projection };
                return true;
            }

            if (matchedProjections.Length > 0 &&
                matchedProjections.All(x => x .NodeType == SqlExpressionType.SubQueryColumn))
            {
                var first = matchedProjections.First();
                var subQueryColumn = first.ColumnExpression as SqlDataSourceColumnExpression
                                    ??
                                    throw new InvalidOperationException($"Expected {nameof(SqlDataSourceColumnExpression)} but got {first.ColumnExpression.GetType().Name}");
                var subQuery = subQueryColumn.DataSource.QuerySource as SqlQueryExpression
                                    ??
                                    throw new InvalidOperationException($"Property '{nameof(SqlDataSourceColumnExpression.DataSource)}' in type '{nameof(SqlDataSourceColumnExpression)}' is not {nameof(SqlQueryExpression)}.");
                var copied = subQuery.CreateCopy();
                var projection = this.CreateSqlColumn(copied, matchedProjections[0].ColumnAlias, matchedProjections[0].ModelPath);
                updatedResult = new[] { projection };
                return true;
            }

            updatedResult = null;
            return false;
        }

        private SqlColumnExpression CreateSqlColumn(SqlExpression sqlExpression, string columnAlias, ModelPath modelPath)
        {
            return this.SqlFactory.CreateColumn(sqlExpression, columnAlias, modelPath);
        }
    }
}
