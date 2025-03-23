using Atis.Expressions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
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

            if (parent is SqlDataSourceReferenceExpression dsRef)
                parent = dsRef.DataSource;

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
                    var columnExpressions = this.MatchWithProjection(path, sqlQuery);
                    if (columnExpressions.Length == 1 && columnExpressions[0].ColumnExpression is SqlQueryExpression otherQuery)
                    {
                        otherQuery = otherQuery.CreateCopy(clearModelMaps: true);
                        result = new SqlColumnExpression[] { new SqlColumnExpression(otherQuery, columnExpressions[0].ColumnAlias, columnExpressions[0].ModelPath) };
                    }
                    else if (columnExpressions.All(x=>x is SqlOuterApplyQueryColumnExpression))
                    {
                        var firstCol = (SqlOuterApplyQueryColumnExpression)columnExpressions.First();
                        var outerApplyQuery = firstCol.OuterApplyQuery.CreateCopy(clearModelMaps: true);
                        result = new SqlColumnExpression[] { new SqlColumnExpression(outerApplyQuery, columnExpressions[0].ColumnAlias, columnExpressions[0].ModelPath) };
                    }
                    else
                        result = columnExpressions;
                    resultSource = sqlQuery;
                }
                else
                {
                    var matchedDataSources = sqlQuery.AllDataSources.Where(x => x.ModelPath.StartsWith(path)).ToArray();
                    var dataSourceReferences = matchedDataSources.Select(x => new SqlDataSourceReferenceExpression(x)).ToArray();
                    result = dataSourceReferences;
                    resultSource = sqlQuery;
                    // x.Field, in-case of if x is a SqlQueryExpression and there is only 1 data source available
                    // above result will have no items, so we need to check if there is any column available
                    if (result.Length == 0)
                    {
                        var dsToUse = sqlQuery.HandleCteOrUsualDataSource(sqlQuery.DefaultDataSource ?? sqlQuery.InitialDataSource);
                        result = this.GetFromDataSource(path, dsToUse);
                        resultSource = dsToUse;
                    }
                }
            }
            else if (parent is SqlCollectionExpression sqlDsCollection && sqlDsCollection.SqlExpressions.Any(x => x is SqlDataSourceReferenceExpression))
            {
                var dsCollection = sqlDsCollection.SqlExpressions.Cast<SqlDataSourceReferenceExpression>()
                                                    .Select(x => x.DataSource as SqlDataSourceExpression)
                                                    .ToArray();
                if (dsCollection.Any(x => x is null))
                    throw new InvalidOperationException($"One or more expressions Collection of {nameof(SqlDataSourceReferenceExpression)} is not {nameof(SqlDataSourceExpression)}");

                if (dsCollection.Length > 1)
                {
                    // it means we have to match in the data sources because there are multiple data sources
                    result = dsCollection.Where(x => x.ModelPath.StartsWith(path))
                                .Select(x => new SqlDataSourceReferenceExpression(x))
                                .ToArray();
                    resultSource = parent;
                }
                else
                {
                    result = GetFromDataSource(path, dsCollection.First());
                    resultSource = dsCollection.First();
                }
            }
            else if (parent is SqlCollectionExpression sqlColCollection && sqlColCollection.SqlExpressions.Any(x => x is SqlColumnExpression))
            {
                var sqlColumns = sqlColCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
                if (sqlColumns.Length > 1)
                {
                    result = sqlColumns.Where(x => x.ModelPath.StartsWith(path)).ToArray();
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
                result = GetFromDataSource(path, parentDs, new ModelPath(path: null));
                resultSource = parentDs;
            }
            else
                throw new InvalidOperationException($"Unknown case");

            if (result.Length == 0)
                throw new InvalidOperationException($"result.Length == 0");
            if (!result.Any(x => (x is SqlDataSourceReferenceExpression) ||
                                (x is SqlColumnExpression)))
                throw new InvalidOperationException($"result does not contain any {nameof(SqlDataSourceReferenceExpression)} or {nameof(SqlColumnExpression)}");

            if (this.IsLeafNode)
            {
                if (result.Length > 1)
                {
                    if (result.Any(x => x is SqlColumnExpression))
                    {
                        result = result.Cast<SqlColumnExpression>()
                                        .Select(x => new SqlColumnExpression(x.ColumnExpression, x.ColumnAlias, x.ModelPath.RemovePrefixPath(path)))
                                        .ToArray();
                    }
                    else if (result.Any(x => x is SqlDataSourceReferenceExpression))
                    {
                        for (var i = 0; i < result.Length; i++)
                        {
                            var ds = (result[i] as SqlDataSourceReferenceExpression).DataSource as SqlDataSourceExpression
                                        ??
                                        throw new InvalidOperationException($"result[{i}] does not contain SqlDataSourceExpression.");
                            var newModelPath = ds.ModelPath.RemovePrefixPath(path);
                            ds.UpdateModelPath(newModelPath.Path);
                            result[i] = new SqlDataSourceReferenceExpression(ds);
                        }
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
                    return new SqlSelectedCollectionExpression(resultSource, result);
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
                    var resultDs = resultAsDsRef.DataSource;
                    if (resultDs is SqlDataSourceExpression sqlQueryDs)
                    {
                        if (sqlQueryDs.DataSource is SqlQueryExpression innerQuery &&
                            innerQuery.Projection.TryGetScalarColumn(out var scalarColExpr))
                            return new SqlDataSourceColumnExpression(sqlQueryDs, scalarColExpr.ColumnAlias);
                        else if (sqlQueryDs.NodeType == SqlExpressionType.OtherDataSource)
                        {
                            // this in-case if other data source directly selected
                            var otherDataSourceSqlQuery = sqlQueryDs.DataSource as SqlQueryExpression
                                                            ??
                                                            throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} but got {sqlQueryDs.DataSource.GetType().Name}");
                            var newSqlQuery = otherDataSourceSqlQuery.CreateCopy(clearModelMaps: true);
                            return newSqlQuery;
                        }
                         
                    }

                    return firstItem;
                }
            }
            else
                return new SqlCollectionExpression(result);
        }

        private SqlExpression[] GetFromDataSource(string[] path, SqlDataSourceExpression ds, ModelPath? dsPath = null)
        {
            SqlExpression[] result;
            dsPath = dsPath ?? ds.ModelPath;
            if (ds.DataSource is SqlTableExpression table)
            {
                var tableColumns = table.TableColumns.Where(x => dsPath.Value.Append(x.ModelPropertyName).StartsWith(path)).ToArray();
                var dataSourceColumns = tableColumns.Select(x => new { x.ModelPropertyName, DsColumn = new SqlDataSourceColumnExpression(ds, x.DatabaseColumnName) });
                var columns = dataSourceColumns.Select(x => new SqlColumnExpression(x.DsColumn, x.ModelPropertyName, ds.ModelPath.Append(x.ModelPropertyName))).ToArray();
                result = columns;
            }
            else if (ds.DataSource is SqlQueryExpression subQuery)
            {
                SqlQueryExpression queryToUse;
                if (subQuery.Projection != null)
                {
                    queryToUse = subQuery;
                }
                else
                {
                    queryToUse = subQuery.InitialDataSource.DataSource as SqlQueryExpression
                                        ??
                                        throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} but got {subQuery.InitialDataSource.DataSource?.GetType().Name}.");
                }

                var matchedProjections = this.MatchWithProjection(path, queryToUse);
                if (matchedProjections.Length == 1 && matchedProjections[0].ColumnExpression is SqlQueryExpression otherQuery)
                {
                    otherQuery = otherQuery.CreateCopy(clearModelMaps: true);
                    result = new SqlColumnExpression[] { new SqlColumnExpression(otherQuery, matchedProjections[0].ColumnAlias, matchedProjections[0].ModelPath) };
                }
                else if (matchedProjections.All(x => x is SqlOuterApplyQueryColumnExpression))
                {
                    var firstCol = (SqlOuterApplyQueryColumnExpression)matchedProjections.First();
                    var outerApplyQuery = firstCol.OuterApplyQuery.CreateCopy(clearModelMaps: true);
                    result = new SqlColumnExpression[] { new SqlColumnExpression(outerApplyQuery, matchedProjections[0].ColumnAlias, matchedProjections[0].ModelPath) };
                }
                else
                {
                    var subQueryColumnAliases = this.MatchWithProjection(path, queryToUse)
                                                        .Select(x => new { DsModel = x.ModelPath, ColExpr = new SqlDataSourceColumnExpression(ds, x.ColumnAlias) })
                                                        .ToArray();
                    var columns = subQueryColumnAliases.Select(x => new SqlColumnExpression(x.ColExpr, x.ColExpr.ColumnName, x.DsModel)).ToArray();
                    result = columns;
                }
            }
            else
                throw new InvalidOperationException($"firstDataSource is neither Table nor a Query");
            return result;
        }

        private SqlColumnExpression[] MatchWithProjection(string[] path, SqlQueryExpression sqlQuery)
        {
            if (sqlQuery.Projection == null)
                throw new InvalidOperationException("Projection is null");

            var collection = sqlQuery.Projection as SqlCollectionExpression
                                ??
                                throw new InvalidOperationException($"Expected {nameof(SqlCollectionExpression)} but got {sqlQuery.Projection.GetType().Name}");
            return collection
                        .SqlExpressions
                        .Cast<SqlColumnExpression>()
                        .Where(x => x.ModelPath.StartsWith(path))
                        .ToArray();
        }
    }
}
