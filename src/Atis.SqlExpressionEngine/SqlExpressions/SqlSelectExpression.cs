using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Exceptions;
using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.Visitors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// 
    /// </summary>
    public partial class SqlSelectExpression : SqlExpression
    {
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Select;

        private readonly List<AliasedDataSource> dataSources = new List<AliasedDataSource>();
        public IReadOnlyCollection<AliasedDataSource> DataSources => dataSources;
        private readonly List<CteDataSource> cteDataSources = new List<CteDataSource>();
        public IReadOnlyCollection<CteDataSource> CteDataSources => cteDataSources;
        private readonly List<SelectColumn> selectList = new List<SelectColumn>();
        public IReadOnlyList<SelectColumn> SelectList => selectList;
        private readonly List<FilterCondition> whereClause = new List<FilterCondition>();
        public IReadOnlyCollection<FilterCondition> WhereClause => whereClause;
        public SqlExpression GroupByClause { get; protected set; }
        private readonly List<FilterCondition> havingClause = new List<FilterCondition>();
        public IReadOnlyCollection<FilterCondition> HavingClause => havingClause;
        private readonly List<OrderByColumn> orderByClause = new List<OrderByColumn>();
        public IReadOnlyCollection<OrderByColumn> OrderByClause => orderByClause;
        public int? Top { get; set; }
        public bool IsDistinct { get; set; }
        public int? RowOffset { get; set; }
        public int? RowsPerPage { get; set; }
        public bool AutoProjection { get; private set; }
        public string Tag { get; set; }
        public ISqlExpressionFactory SqlFactory { get; }
        // QueryShape property cannot be other than SqlExpression because
        //      db.Table1.GroupBy(x => x.Field1).Select(x => x.Key).Select(g => g + 1)
        // In above case `g` parameter must be resolved to a SqlDataSourceColumnExpression not anything else.
        // So default mapping of a ParameterExpression would be directly to QueryShape property
        // because most of the time this will be true, except for certain cases where we need to
        // map it a bit differently for example,
        //      db.Table1.GroupBy(x => x.Field1).Select(x => x.Key).LeftJoin(db.Table2, (o, j) => new { o, j }, ...)
        // In above example, `o` in the NewExpression must NOT be resolved as SqlDataSourceColumnExpression, so in
        // these specific cases during the LambdaExpression Parameter mapping we will NOT map the QuerySource
        // property with ParameterExpression, rather we will create another expression on the fly to map.
        protected SqlExpression QueryShape { get; set; }

        private SqlSelectExpression(IEnumerable<CteDataSource> cteDataSources, IEnumerable<AliasedDataSource> dataSources, IEnumerable<SelectColumn> selectList, IEnumerable<FilterCondition> whereClause, SqlExpression groupByClause, IEnumerable<FilterCondition> havingClause, IEnumerable<OrderByColumn> orderByClause, int? top, bool isDistinct, int? rowOffset, int? rowsPerPage, bool autoProjection, string tag, SqlExpression queryShape, ISqlExpressionFactory sqlFactory)
        {
            if (cteDataSources != null)
                this.cteDataSources.AddRange(cteDataSources);
            if (dataSources != null)
                this.dataSources.AddRange(dataSources);
            if (selectList != null)
                this.selectList.AddRange(selectList);
            if (whereClause != null)
                this.whereClause.AddRange(whereClause);
            this.GroupByClause = groupByClause;
            if (havingClause != null)
                this.havingClause.AddRange(havingClause);
            if (orderByClause != null)
                this.orderByClause.AddRange(orderByClause);
            this.Top = top;
            this.IsDistinct = isDistinct;
            this.RowOffset = rowOffset;
            this.RowsPerPage = rowsPerPage;
            this.AutoProjection = autoProjection;
            this.Tag = tag;
            this.QueryShape = queryShape;
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));
        }

        public SqlSelectExpression(CteDataSource[] cteDataSources, SqlExpression selectedSource, ISqlExpressionFactory sqlFactory)
        {
            if (selectedSource is null)
                throw new ArgumentNullException(nameof(selectedSource));
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));

            if (cteDataSources?.Length > 0)
            {
                this.cteDataSources.AddRange(cteDataSources);
            }

            var queryShapeComposer = new QueryShapeComposer(this);
            this.QueryShape = queryShapeComposer.ComposeQueryShape(selectedSource);
        }

        public SqlSelectExpression CreateCopy()
        {
            return SqlSelectExpressionCopyMaker.CreateCopy(this);
        }

        public void RemoveGrouping()
        {
            if (this.HasProjectionApplied)
                throw new InvalidOperationException($"Projection has been applied, cannot remove grouping.");
            this.GroupByClause = null;
        }

        // TODO: we haven't finalized but we are making this class as non-visitable
        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            // we are NOT doing any further visiting
            return this;
            //throw new InvalidOperationException($"{this.GetType().Name} does not support visitor pattern.");
        }

        public void ApplyWhere(SqlExpression predicate, bool useOrOperator)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            predicate = this.WrapIfRequiredSingle(SqlQueryOperation.Where, predicate);
            this.whereClause.Add(new FilterCondition(predicate, useOrOperator));
            this.OnAfterApply(SqlQueryOperation.Where);
        }

        public void ApplyWhereMultipleFields(SqlExpression predicateShapeLeft, SqlExpression predicateShapeRight)
        {
            if (predicateShapeLeft is null)
                throw new ArgumentNullException(nameof(predicateShapeLeft));
            if (predicateShapeRight is null)
                throw new ArgumentNullException(nameof(predicateShapeRight));
            var joinCondition = this.SqlFactory.CreateJoinCondition(predicateShapeLeft, predicateShapeRight);
            this.ApplyWhere(joinCondition, useOrOperator: false);
        }

        public void ApplyHaving(SqlExpression predicate, bool useOrOperator)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            predicate = this.WrapIfRequiredSingle(SqlQueryOperation.Where, predicate);
            this.havingClause.Add(new FilterCondition(predicate, useOrOperator));
            this.OnAfterApply(SqlQueryOperation.Where);
        }

        public void ApplyGroupBy(SqlExpression sqlExpression)
        {
            if (sqlExpression is null)
                throw new ArgumentNullException(nameof(sqlExpression));

            sqlExpression = this.WrapIfRequiredSingle(SqlQueryOperation.GroupBy, sqlExpression);
            this.GroupByClause = sqlExpression;
            this.OnAfterApply(SqlQueryOperation.GroupBy);
        }

        public void ApplyProjection(SqlExpression projection)
        {
            if (projection is null)
                throw new ArgumentNullException(nameof(projection));

            projection = this.WrapIfRequiredSingle(SqlQueryOperation.Select, projection);

            if (this.selectList.Count > 0)
                throw new InvalidOperationException("Select list already has been defined.");

            projection = this.ConvertDerivedTableToOuterApplyInProjection(projection, memberName: null);

            this.QueryShape = projection;
            this.ApplyProjectionFromQueryShape(this.QueryShape, applyAll: false);
        }

        public SqlExpression GetQueryShapeForFieldMapping() 
            => this.QueryShape is SqlQueryShapeExpression qs ? new SqlQueryShapeFieldResolverExpression(qs, this) : this.QueryShape;

        public SqlQueryShapeExpression GetQueryShapeForDataSourceMapping()
        {
            if (this.HasProjectionApplied)
                throw new InvalidOperationException($"Projection has been applied, query should be wrapped before calling this method.");
            // we are NOT returning the query shape directly because this is possible that the 
            // QueryShape is a QueryDataSourceShapeExpression, so we are creating one
            if (this.dataSources.Count == 1)
            {
                if (this.QueryShape is SqlDataSourceQueryShapeExpression qds)
                    return qds;
                else
                    return new SqlDataSourceQueryShapeExpression(this.QueryShape, this.dataSources[0].Alias);
            }
            // if there are more than 1 data sources and QueryShape is something else then it's a problem
            // and must be looked into and see what are the cases
            return this.QueryShape as SqlQueryShapeExpression
                    ??
                     throw new InvalidOperationException($"QueryShape is not a {nameof(SqlQueryShapeExpression)}.");

            //      db.From(() => new { t1 = db.Table1, t2 = db.Table2 })
            //           .Select(x => new { x.t1.Field1, x.t2.Field2 } )
            //           .LeftJoin(db.Table3, (o, j) => new { o, j }, s => s.o.t1.Field1 == s.j.FieldX)
            // 
            // In above example, before we can convert 2nd argument of LeftJoin, previous query must be wrapped.
            // Assuming query is wrapped, so when we will reach in the 2nd argument, the `o` needs to be mapped with
            // a SqlExpression, so we'll reach in this method, and at that time the HasProjectionApplied will be false
            // and DataSources.Count = 1, so we'll return, this.QueryShape [SqlMemberInitExpression] along with 1st data source alias
            //
            //      db.Table1.GroupBy(x => x.Field1).Select(x => x.Key)
            //                .LeftJoin(db.Table2, (o, j) => new { o, j }, s => s.o == s.j.FieldX)
            // 
            // In above example, again the query will be wrapped and we'll reach up to `o` for 2nd argument,
            // and we'll land in this method, HasProjectionApplied = false and DataSourcesCount = 1, so we'll return
            // this.QueryShape [SqlDataSourceColumnExpression] along with 1st data source alias,
            // and when 2nd argument is completed the QueryShape will be changed to
            //
            //  SqlMemberInitExpression [Bindings[0] = { "o", SqlDataSourceQueryShapeExpression }, Bindings[1] = { "j", SqlDataSourceQueryShapeExpression } ]
            //
            // Then we'll move to 3rd argument and we'll face s which will return MemberInitExpression (QueryShape) and then
            // we'll receive `s.o` in MemberExpressionConverter which will resolve `o` from previous expression and will return
            // SqlDataSourceColumnExpression. <- this is a problem because s.o will NOT be resolved to SqlDataSourceColumnExpression
            // it will be resolved to SqlDataSourceQueryShapeExpression, we thought we could change the MemberExpressionConverter to
            // check if it's a SqlDataSourceQueryShapeExpression then check if it's scalar then return the underlying ShapeExpression
            // but this is wrong, because we might be using similar MemberExpression as 1st arg of Join, e.g. LeftJoin(x => x.o, ...),
            // In this case it should be resolved to SqlDataSourceQueryShapeExpression.
            // So to handle above problem we have GetQueryShapeForFieldMapping() method above, that method returns a special SqlQueryShapeExpression
            // that always resolve to internal leaf node expression for scalar query shape.
        }

        private SqlExpression ConvertDerivedTableToOuterApplyInProjection(SqlExpression projection, string memberName)
        {
            if (projection is SqlMemberInitExpression memberInit)
            {
                var memberAssignments = new List<SqlMemberAssignment>();
                var memberAssignmentChanged = false;
                foreach (var binding in memberInit.Bindings)
                {
                    var updatedExpression = this.ConvertDerivedTableToOuterApplyInProjection(binding.SqlExpression, binding.MemberName);
                    if(updatedExpression != binding.SqlExpression)
                        memberAssignmentChanged = true;
                    memberAssignments.Add(new SqlMemberAssignment(binding.MemberName, updatedExpression));
                }
                if (memberAssignmentChanged)
                    return new SqlMemberInitExpression(memberAssignments);
                else
                    return memberInit;
            }
            else if (projection is SqlDerivedTableExpression derivedTable &&
                    this.ShouldSubQueryBeProjectedAsJoin(derivedTable))
            {
                var dsShape = this.AddDataSourceWithJoinResolution(derivedTable, isDefaultIfEmpty: true, memberName);
                return dsShape.ShapeExpression;
            }
            else
                return projection;
        }

        private bool ShouldSubQueryBeProjectedAsJoin(SqlDerivedTableExpression derivedTable)
        {
            if (derivedTable.NodeType == SqlExpressionType.DataManipulationDerivedTable)
                return false;
            if (derivedTable.SelectColumnCollection?.SelectColumns.All(x => x.ScalarColumn) ?? false)
                // if all are scalar columns then cannot be outer apply
                return false;
            else
                // if there are no scalar columns then it must be outer apply
                return true;
        }

        public void ApplyOrderBy(SqlExpression orderByExpression, SortDirection direction)
        {
            if (orderByExpression is null)
                throw new ArgumentNullException(nameof(orderByExpression));

            orderByExpression = this.WrapIfRequiredSingle(SqlQueryOperation.OrderBy, orderByExpression);
            var matchedExpression = this.selectList.Where(x => x.ColumnExpression == orderByExpression).FirstOrDefault();
            if (matchedExpression != null)
                orderByExpression = this.SqlFactory.CreateAlias(matchedExpression.Alias);
            this.orderByClause.Add(new OrderByColumn(orderByExpression, direction));
            this.OnAfterApply(SqlQueryOperation.OrderBy);
        }

        public void ApplyTop(int top)
        {
            this.WrapIfRequired(SqlQueryOperation.Top, null);
            this.Top = top;
            this.OnAfterApply(SqlQueryOperation.Top);
        }

        public void ApplyDistinct()
        {
            this.WrapIfRequired(SqlQueryOperation.Distinct, null);
            this.IsDistinct = true;
            this.OnAfterApply(SqlQueryOperation.Distinct);
        }

        public void ApplyPaging(int pageNumber, int pageSize)
        {
            if (!this.HasOrderByApplied)
                this.ApplyOrderBy(new SqlLiteralExpression(1), SortDirection.Ascending);
            this.WrapIfRequired(SqlQueryOperation.RowsPerPage, null);
            this.RowOffset = (pageNumber - 1) * pageSize;
            this.RowsPerPage = pageSize;
            this.OnAfterApply(SqlQueryOperation.RowsPerPage);
        }

        public void ApplyRowsPerPage(int rowsPerPage)
        {
            this.WrapIfRequired(SqlQueryOperation.RowsPerPage, null);
            this.RowsPerPage = rowsPerPage;
            this.OnAfterApply(SqlQueryOperation.RowsPerPage);
        }

        public void ApplyRowOffset(int rowOffset)
        {
            this.WrapIfRequired(SqlQueryOperation.RowOffset, null);
            this.RowOffset = rowOffset;
            this.OnAfterApply(SqlQueryOperation.RowOffset);
        }

        public SqlDataSourceQueryShapeExpression AddJoin(SqlQuerySourceExpression querySource, SqlJoinType joinType)
        {
            return this.AddJoinedSource(SqlQueryOperation.Join, querySource, joinType);
        }

        public bool TryResolveNavigationDataSource(SqlQueryShapeExpression parentQueryShape, string memberName, out SqlExpression assignment)
        {
            return parentQueryShape.TryResolveMember(memberName, out assignment);
        }

        //public SqlExpression GetDataSourceQueryShape(Guid dataSourceAlias)
        //{
        //    var (dataSource, _) = this.GetAliasedDataSourceRequired(dataSourceAlias);
        //    return dataSource.CreateQueryShape();
        //}

        public SqlDataSourceQueryShapeExpression AddNavigationJoin(SqlQueryShapeExpression navigationParent, SqlExpression joinedSource, SqlJoinType joinType, string navigationName)
        {
            if (navigationParent is null)
                throw new ArgumentNullException(nameof(navigationParent));
            if (joinedSource is null)
                throw new ArgumentNullException(nameof(joinedSource));
            if (string.IsNullOrWhiteSpace(navigationName))
                throw new ArgumentException($"Navigation Name cannot be empty.", nameof(navigationName));
            
            if (this.TryResolveNavigationDataSource(navigationParent, navigationName, out _))
                throw new InvalidOperationException($"Navigation join already exists for the given navigation name '{navigationName}'.");

            SqlQuerySourceExpression sqlQuerySource;
            if (joinedSource is SqlTableExpression table)
            {
                sqlQuerySource = table;
            }
            else
            {
                var derivedTable = joinedSource as SqlDerivedTableExpression
                                         ??
                                         throw new InvalidOperationException($"Expected a {nameof(SqlDerivedTableExpression)} but got {joinedSource.GetType().Name}");

                if (joinType != SqlJoinType.CrossApply && joinType != SqlJoinType.OuterApply)
                    sqlQuerySource = derivedTable.ConvertToTableIfPossible();
                else
                    sqlQuerySource = derivedTable;
            }
            var dataSource = this.AddJoinedSource(SqlQueryOperation.NavigationJoin, sqlQuerySource, joinType, navigationParent, navigationName: navigationName, joinName: navigationName);
            return dataSource;
        }

        // TODO: check if we can move isDefaultEmpty logic inside here as well
        public SqlDataSourceQueryShapeExpression AddDataSourceWithJoinResolution(SqlQuerySourceExpression newQuerySource, bool isDefaultIfEmpty, string tag = null)
        {
            // This method is called through SelectMany converter where SelectMany has received an external query
            // and it is sending that external query to this method.
            SqlJoinType joinType = SqlJoinType.Cross;
            SqlExpression joinCondition = null;
            Guid? newDataSourceAlias = null;
            SqlQueryOperation newOperation = SqlQueryOperation.Join;

            // This method is being called from SelectMany Converter and if this is the case that before SelectMany there
            // was GroupJoin present, then we are removing the separate derived table that was added as a result of GroupJoin
            // from the modelBinding.
            //
            //      from t1 in table1
            //      join t2 in table2 on t1.Id equals t2.Id into g
            //      from t2 in g.DefaultIfEmpty()
            //      select new { t1.Id, t1.Field1, t2.Field2 }
            //

            // TODO: see if we can come up with non-projectable modelBinding so that we don't have to remove this
            //var tableUsedInModelBinding = this.modelBinding.GetBindings().Where(x => x.SqlExpression == newQuerySource).FirstOrDefault();
            //if (tableUsedInModelBinding != null)
            //    this.modelBinding.Remove(tableUsedInModelBinding);

            // if external query is a derived table
            if (newQuerySource is SqlDerivedTableExpression derivedTable)
            {
                // We will provide this tempAlias for JoinableQueryBuilder which will replace the outer query aliases
                // with this tempAlias, if JoinQueryBuilder suggests that inner join is not possible then this
                // tempAlias will not be used any further.
                var tempAlias = Guid.NewGuid();
                // We'll try to check if that derived table can be added as Inner join instead of Cross Join
                var joinableQueryBuilder = new JoinableQueryBuilder(derivedTable, new HashSet<Guid>(this.dataSources.Select(x => x.Alias)), this.SqlFactory, tempAlias);
                if (joinableQueryBuilder.TryBuild(out var builderResult))
                {
                    // Ok we are here it means we can add the derived table as inner join.
                    // Now we are checking if the incoming derived table can be converted to simple Table
                    // because this is possible that the query only had 1 where clause and that was actually
                    // the join condition which JoinQueryBuilder has already extracted in JoinConditions.
                    newQuerySource = builderResult.NormalizedDerivedTable.ConvertToTableIfPossible();
                    joinCondition = builderResult.JoinCondition;
                    // Since JoinableQueryBuilder has successfully created the join conditions and 
                    // has normalized the given newQuerySource, we can now use the tempAlias when
                    // we add newQuerySource in this SqlSelectExpression.
                    newDataSourceAlias = tempAlias;
                    joinType = joinCondition != null ? SqlJoinType.Inner : SqlJoinType.Cross;
                    tag = tag ?? derivedTable.Tag;
                    newOperation = string.IsNullOrWhiteSpace(derivedTable.Tag) ? SqlQueryOperation.Join : SqlQueryOperation.NavigationJoin;
                }
                else
                {
                    if (ExternalDataSourceUsageVisitor.HasExternalDataSourceBeenUsed(derivedTable))
                        joinType = SqlJoinType.CrossApply;
                    if (joinType != SqlJoinType.CrossApply)
                        newQuerySource = derivedTable.ConvertToTableIfPossible();
                }
            }

            if (isDefaultIfEmpty)
            {
                switch (joinType)
                {
                    case SqlJoinType.Inner:
                        joinType = SqlJoinType.Left;
                        break;
                    case SqlJoinType.CrossApply:
                        joinType = SqlJoinType.OuterApply;
                        break;
                }
            }

            // We are passing `newDataSourceAlias` in below method which will be null in normal scenarios,
            // however, in-case if the given `newQuerySource` can be inner joined in this SqlSelectExpression
            // then `newDataSourceAlias` will be set above and will not be null in that case.

            // `joinCondition` will not be null if `newQuerySource` can be added as inner join.
            //var queryShape = this.QueryShape as SqlDataSourceQueryShapeExpression
            //                    ??
            //                    throw new InvalidOperationException($"QueryShape is not a {nameof(SqlDataSourceQueryShapeExpression)}.");
            var dataSource = this.AddJoinedSource(newOperation, newQuerySource, joinType, parentShape: null, navigationName: null, dataSourceAlias: newDataSourceAlias, joinCondition: joinCondition, joinName: tag);
            return dataSource;
        }

        public void ConvertToRecursiveQuery(SqlDerivedTableExpression anchorDerivedTable, SqlDerivedTableExpression recursiveDerivedTable)
        {
            var cteAlias = Guid.NewGuid();
            var cteReference = new SqlCteReferenceExpression(cteAlias);
            recursiveDerivedTable = SqlExpressionReplacementVisitor.Find(anchorDerivedTable).In(recursiveDerivedTable).ReplaceWith(cteReference) as SqlDerivedTableExpression
                                    ??
                                    throw new InvalidOperationException($"Replacement Visitor didn't convert back to {nameof(SqlDerivedTableExpression)}.");
            var unionItem1 = new UnionItem(anchorDerivedTable, SqlUnionType.UnionAll);  // 1st UnionAll is not really used
            var unionItem2 = new UnionItem(recursiveDerivedTable, SqlUnionType.UnionAll);
            var unionAll = this.SqlFactory.CreateUnionQuery(new[] { unionItem1, unionItem2 });

            this.InitializeCteQuery(cteReference, unionAll);
        }

        private void InitializeCteQuery(SqlCteReferenceExpression cteReference, SqlSubQuerySourceExpression querySource)
        {
            this.Initialize();

            var cteDataSource = new CteDataSource(querySource, cteReference.CteAlias);
            this.cteDataSources.Add(cteDataSource);

            var fromSource = new AliasedDataSource(cteReference, Guid.NewGuid());
            AddAliasedDataSource(fromSource);

            this.QueryShape = querySource.CreateQueryShape(fromSource.Alias);
        }

        public void SwitchBindingToLastDataSource()
        {
            var lastDataSource = this.dataSources.Last();
            this.QueryShape = this.CreateDataSourceQueryShape(lastDataSource);
        }

        public void UpdateJoin(Guid joinDataSourceAlias, SqlJoinType joinType, SqlExpression joinCondition, string joinName, bool navigationJoin)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            this.dataSources[indexOfDataSource] = new JoinDataSource(joinType, dataSource.QuerySource, joinDataSourceAlias, joinCondition: joinCondition, joinName: joinName, isNavigationJoin: navigationJoin);
        }

        public void UpdateJoinType(Guid joinDataSourceAlias, SqlJoinType joinType)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                this.dataSources[indexOfDataSource] = new JoinDataSource(joinType, joinDataSource.QuerySource, joinDataSourceAlias, joinCondition: joinDataSource.JoinCondition, joinName: joinDataSource.JoinName, isNavigationJoin: joinDataSource.IsNavigationJoin);
            else
                throw new InvalidOperationException($"Join data source with alias {joinDataSourceAlias} is not a join data source.");
        }

        public void UpdateJoinCondition(Guid joinDataSourceAlias, SqlExpression joinCondition)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                this.dataSources[indexOfDataSource] = new JoinDataSource(joinDataSource.JoinType, joinDataSource.QuerySource, joinDataSource.Alias, joinCondition: joinCondition, joinName: joinDataSource.JoinName, isNavigationJoin: joinDataSource.IsNavigationJoin);
            else
                throw new InvalidOperationException($"Join data source with alias {joinDataSourceAlias} is not a join data source.");
        }

        public void UpdateJoinName(Guid joinDataSourceAlias, string joinName)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                this.dataSources[indexOfDataSource] = new JoinDataSource(joinDataSource.JoinType, joinDataSource.QuerySource, joinDataSource.Alias, joinCondition: joinDataSource.JoinCondition, joinName: joinName, isNavigationJoin: joinDataSource.IsNavigationJoin);
            else
                throw new InvalidOperationException($"Join data source with alias {joinDataSourceAlias} is not a join data source.");
        }

        private SqlExpression WrapIfRequiredSingle(SqlQueryOperation newOperation, SqlExpression sqlExpression)
        {
            if (sqlExpression is null)
            {
                this.WrapIfRequired(newOperation, null);
                return null;
            }
            else
            {
                return this.WrapIfRequired(newOperation, new[] { sqlExpression })?.FirstOrDefault()
                       ??
                       throw new InvalidOperationException($"WrapIfRequired didn't return any expression.");
            }
        }

        public void WrapIfRequired(SqlQueryOperation newOperation) => this.WrapIfRequired(newOperation, null);

        protected SqlExpression[] WrapIfRequired(SqlQueryOperation newOperation, SqlExpression[] sqlExpressions)
        {
            if (sqlExpressions?.Length > 0)
            {
                // below method will check if given expression is a sub-query (SqlDerivedTableExpression)
                // and it is CTE then it modifies the current query and extract the CTE data sources
                // from sub-query and it in this query and make this query as CTE query.
                sqlExpressions = this.WrapQueryIfCteReferencesExist(sqlExpressions);
            }

            if (this.IsWrapRequired(newOperation))
            {
                this.WrapQuery();

                if (sqlExpressions?.Length > 0)
                {
                    sqlExpressions = this.ReplaceDataSourceAccessing(sqlExpressions);
                }
            }

            return sqlExpressions;
        }

        protected virtual void WrapQuery()
        {
            this.ApplyAutoProjectionIfPossible(applyAll: false);

            var currentCteDataSources = this.cteDataSources.ToArray();
            for (var i = 0; i < currentCteDataSources.Length; i++)
                this.cteDataSources.Remove(currentCteDataSources[i]);

            this.AppendSelectListWithColumnsUsedInSubQuery();

            var queryableSelectedInProjection = this.selectList.Where(x => x.ColumnExpression is SqlQueryableExpression).ToArray();

            foreach (var queryable in queryableSelectedInProjection)
                this.selectList.Remove(queryable);

            var derivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(this);

            this.Initialize();
            
            var derivedTableDataSource = new AliasedDataSource(derivedTable, Guid.NewGuid());
            AddAliasedDataSource(derivedTableDataSource);

            this.QueryShape = this.CreateDataSourceQueryShape(derivedTableDataSource);

            if (queryableSelectedInProjection.Length > 0 &&
                    this.QueryShape is SqlQueryShapeExpression queryShape)
            {
                SqlMemberInitExpression memberInit;
                if (queryShape is SqlDataSourceQueryShapeExpression dsQs)
                    memberInit = dsQs.ShapeExpression as SqlMemberInitExpression
                                ??
                                throw new InvalidOperationException($"QueryShape is not a {nameof(SqlMemberInitExpression)}.");
                else if (queryShape is SqlMemberInitExpression mi)
                    memberInit = mi;
                else
                    throw new InvalidOperationException($"QueryShape is not a {nameof(SqlMemberInitExpression)}.");

                foreach (var queryable in queryableSelectedInProjection)
                {
                    var updatedExpression = this.ReplaceDataSourceAccessingSingle(queryable.ColumnExpression);
                    memberInit.AddMemberAssignment(queryable.Alias, updatedExpression, projectable: true);
                }
            }

            for (var i = 0; i < currentCteDataSources.Length; i++)
                this.cteDataSources.Add(currentCteDataSources[i]);
        }

        protected void OnAfterApply(SqlQueryOperation operationApplied)
        {
            // Since navigation is added to the query and the binding remains in the modelBinding,
            // which causes problem in next apply, for example, Navigation is used in Where
            // and then Top is applied, after than Select is applied which will cause the 
            // query to wrap, but it will again find the Navigation in modelBinding so
            // it will NOT add navigation, that's why we are removing the navigation data source from
            // modelBinding if NavigationJoin will cause the wrapping.
            if (this.IsWrapRequired(SqlQueryOperation.NavigationJoin))      // if next navigation join will cause the wrapping
            {
                // TODO: check if we need to remove non-projectable members from QueryShape
            }
        }

        public virtual bool HasJoinApplied => this.dataSources.Where(x => x is JoinDataSource).Any();
        public virtual bool HasProjectionApplied => this.selectList.Count > 0;
        public virtual bool HasWhereApplied => this.whereClause.Count > 0;
        public virtual bool HasGroupByApplied => this.GroupByClause != null;
        public virtual bool HasHavingApplied => this.havingClause.Count > 0;
        public virtual bool HasOrderByApplied => this.orderByClause.Count > 0;
        public virtual bool HasTopApplied => this.Top.HasValue;
        public virtual bool HasDistinctApplied => this.IsDistinct;
        public virtual bool HasRowOffsetApplied => this.RowOffset.HasValue;
        public virtual bool HasRowsPerPageApplied => this.RowsPerPage.HasValue;

        protected virtual bool IsWrapRequired(SqlQueryOperation newOperation)
        {
            var performWrap = false;
            switch (newOperation)
            {
                case SqlQueryOperation.Select:
                    performWrap = this.HasProjectionApplied || this.HasDistinctApplied || this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.Join:
                    performWrap = this.HasProjectionApplied || this.HasWhereApplied || this.HasGroupByApplied || this.HasHavingApplied || this.HasDistinctApplied || this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.NavigationJoin:
                    performWrap = this.HasProjectionApplied || this.HasGroupByApplied || this.HasHavingApplied || this.HasDistinctApplied || this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.Where:
                    performWrap = this.HasProjectionApplied || this.HasDistinctApplied || this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.OrderBy:
                    performWrap = this.HasDistinctApplied || this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.GroupBy:
                    performWrap = this.HasProjectionApplied || this.HasDistinctApplied || this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.Top:
                    performWrap = this.HasTopApplied || this.HasDistinctApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.Distinct:
                    performWrap = this.HasDistinctApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.RowOffset:
                    performWrap = this.HasTopApplied || this.HasRowOffsetApplied || this.HasRowsPerPageApplied;
                    break;
                case SqlQueryOperation.RowsPerPage:
                    performWrap = this.HasTopApplied || this.HasRowsPerPageApplied;
                    break;
            }
            return performWrap;
        }

        protected virtual void Initialize()
        {
            this.dataSources.Clear();
            this.selectList.Clear();
            this.whereClause.Clear();
            this.havingClause.Clear();
            this.orderByClause.Clear();
            this.GroupByClause = null;
            this.Top = null;
            this.IsDistinct = false;
            this.RowOffset = null;
            this.RowsPerPage = null;
            this.AutoProjection = false;
            // TODO: check if it's ok
            this.QueryShape = null;
        }


        public void UpdateModelBinding(SqlExpression newQueryShape)
        {
            if (newQueryShape is null)
                throw new ArgumentNullException(nameof(newQueryShape));

            // TODO: check if below will work, because we are NOT using QueryShapeComposer
            //var queryShape = this.QueryShape as SqlQueryShapeExpression
            //                    ??
            //                    throw new InvalidOperationException($"{nameof(SqlSelectExpression)}.{nameof(QueryShape)} is not of type {nameof(SqlQueryShapeExpression)}");
            //queryShape.Reset(newQueryShape);
            this.QueryShape = newQueryShape;
        }




        private SqlDataSourceQueryShapeExpression AddJoinedSource(SqlQueryOperation newOperation, SqlQuerySourceExpression querySource, SqlJoinType joinType, SqlQueryShapeExpression parentShape = null, string navigationName = null, Guid? dataSourceAlias = null, SqlExpression joinCondition = null, string joinName = null)
        {
            if (querySource is null)
                throw new ArgumentNullException(nameof(querySource));

            // It is important that we pass `NavigationJoin` as the next operation instead of `Join` when `navigationName`
            // is provided. This handles cases where a "Where" clause is applied to the query, followed by a projection
            // that involves navigation.
            // Example:
            //      var q = dbc.Table1.Where(x => x.Field1 == "123").Select(x => new { x.NavProp1().FieldX, x.Field2 });
            // In the above case, if we treat the navigation as a normal join, the Navigation Property in Select
            // would cause the query to wrap unnecessarily. By treating it as a navigation join, the wrapping mechanism
            // ensures that the Join is applied without additional wrapping.

            SqlExpression[] expressionsToCheckIfWrappingOccurs;
            if (joinCondition != null)
                expressionsToCheckIfWrappingOccurs = new[] { querySource, joinCondition };
            else
                expressionsToCheckIfWrappingOccurs = new[] { querySource };
            expressionsToCheckIfWrappingOccurs = this.WrapIfRequired(newOperation, expressionsToCheckIfWrappingOccurs);

            // EXTREMELY IMPORTANT:
            //  below we are setting back `querySource` and `joinCondition` from `sqlExpressionsToPass`
            //  this is extremely important, otherwise the provided parameters will NOT be replaced
            //  if wrapping happened

            querySource = expressionsToCheckIfWrappingOccurs[0] as SqlQuerySourceExpression
                                ??
                                throw new InvalidOperationException($"Expected expression type is '{nameof(SqlQuerySourceExpression)}'.");
            joinCondition = expressionsToCheckIfWrappingOccurs.Length > 1 ? expressionsToCheckIfWrappingOccurs[1] : null;

            var aliasedDataSource = new JoinDataSource(joinType, querySource, dataSourceAlias ?? Guid.NewGuid(), joinCondition: joinCondition, joinName: joinName, isNavigationJoin: newOperation == SqlQueryOperation.NavigationJoin);            
            // here we will initialize new QueryModelBinding for the aliased data source and
            // add the data source's columns in it
            this.AddAliasedDataSource(aliasedDataSource);

            var dsQueryShape = this.CreateDataSourceQueryShape(aliasedDataSource);

            if (!string.IsNullOrWhiteSpace(navigationName))
            {
                if (parentShape is null)
                    throw new InvalidOperationException($"Action is NavigationJoin, parentShape is required");
                parentShape.AddMemberAssignment(navigationName, dsQueryShape, projectable: false);
            }

            this.OnAfterApply(newOperation);

            return dsQueryShape;
        }

        private SqlExpression ReplaceDataSourceAccessingSingle(SqlExpression sqlExpression)
        {
            if (sqlExpression is null)
                throw new ArgumentNullException(nameof(sqlExpression));
            return this.ReplaceDataSourceAccessing(new[] { sqlExpression }).FirstOrDefault()
                   ??
                   throw new InvalidOperationException($"ReplaceDataSourceAccessing didn't return any result");
        }

        protected SqlExpression[] ReplaceDataSourceAccessing(SqlExpression[] sqlExpressions)
        {
            if (sqlExpressions is null)
                throw new ArgumentNullException(nameof(sqlExpressions));

            if (this.dataSources.Count > 1)
            {
                var cteDataSourceCount = this.cteDataSources.Count();
                if (cteDataSourceCount + 1 != this.dataSources.Count)
                    throw new InvalidOperationException($"This method should be called right after the wrapping is done, currently we are seeing more than 1 data sources which means some other operation has also been performed before calling this method.");
            }

            var ds = this.dataSources.First();
            SqlDerivedTableExpression derivedTable;
            if (ds.QuerySource is SqlDerivedTableExpression dv)
            {
                derivedTable = dv;
            }
            else if (ds.QuerySource is SqlCteReferenceExpression cteRef)
            {
                var cteDataSource = this.cteDataSources.Where(x => x.CteAlias == cteRef.CteAlias).First();

                // TODO: probably need to see other cases where CTE contains a Union maybe
                derivedTable = cteDataSource.CteBody as SqlDerivedTableExpression
                                ??
                                throw new InvalidOperationException($"CTE Data Source body does not contain {nameof(SqlDerivedTableExpression)}.");
            }
            else
                throw new InvalidOperationException($"Data source is neither a derived table nor CTE reference.");

            var derivedTableProjections = derivedTable.SelectColumnCollection.SelectColumns.Where(x => !(x.ColumnExpression is SqlQueryableExpression)).ToArray();
            // Since SqlDerivedTableExpression has this condition that it's SelectList cannot be null / empty, therefore,
            // we are guaranteed that we will have at least 1 column in the derivedTable.
            // This query has been wrapped, so the actual query has been moved inside as derived table,
            // so the select list expression should have same expressions as sqlExpression
            for (var i = 0; i < sqlExpressions.Length; i++)
            {
                sqlExpressions[i] = SubQueryProjectionReplacementVisitor.FindAndReplace(derivedTableProjections, ds, sqlExpressions[i]);
            }
            return sqlExpressions;
        }

        private SqlDataSourceQueryShapeExpression CreateDataSourceQueryShape(AliasedDataSource aliasedDataSource)
        {
            if (aliasedDataSource is null)
                throw new ArgumentNullException(nameof(aliasedDataSource));
            if (aliasedDataSource.QuerySource is SqlCteReferenceExpression cteRef)
            {
                var cteDataSource = this.cteDataSources.Where(x => x.CteAlias == cteRef.CteAlias).FirstOrDefault()
                                    ??
                                    throw new InvalidOperationException($"CTE Data Source with alias {cteRef.CteAlias} not found.");
                return cteDataSource.CteBody.CreateQueryShape(aliasedDataSource.Alias);
            }
            else
                return aliasedDataSource.QuerySource.CreateQueryShape(aliasedDataSource.Alias);
        }

        
        private (AliasedDataSource DataSource, int Index) GetAliasedDataSourceRequired(Guid dataSourceAlias)
        {
            var indexOfDataSource = this.dataSources.FindIndex(x => x.Alias == dataSourceAlias);
            if (indexOfDataSource < 0)
                throw new ArgumentException($"Data Source with alias {dataSourceAlias} not found.", nameof(dataSourceAlias));
            return (this.dataSources[indexOfDataSource], indexOfDataSource);
        }

        private void AddAliasedDataSource(AliasedDataSource aliasedDataSource)
        {
            // add newly created aliased data source in our data source collection
            this.dataSources.Add(aliasedDataSource);
        }

        private bool ApplyAutoProjectionIfPossible(bool applyAll)
        {
            if (this.selectList.Count > 0)
                return false;
            this.ApplyProjectionFromQueryShape(this.GroupByClause ?? this.QueryShape, applyAll: applyAll);
            this.AutoProjection = true;
            return true;
        }

        private void ApplyProjectionFromQueryShape(SqlExpression queryShape, bool applyAll)
        {
            if (this.selectList.Count != 0)
                throw new InvalidOperationException($"Projection has already been applied");
            var selectList = ExtensionMethods.ConvertQueryShapeToSelectList(queryShape, applyAll);
            // after this we must NOT have any SqlMemberInitExpression and SqlDataSourceQueryShapeExpression
            // within select expression
            this.selectList.AddRange(selectList);
            this.OnAfterApply(SqlQueryOperation.Select);
        }

        //public IReadOnlyList<SelectColumn> GetProjection()
        //{
        //    if (this.HasProjectionApplied || this.GroupByClause == null)
        //        return ExtensionMethods.ConvertQueryShapeToSelectList(this.QueryShape, applyAll: false);
        //    else
        //    {
        //        return ExtensionMethods.ConvertQueryShapeToSelectList(this.GroupByClause, applyAll: false);
        //    }
        //}


        private void AppendSelectListWithColumnsUsedInSubQuery()
        {
            // Step 1: Skip if no projection exists
            if (this.selectList.Count == 0)
                return;

            // Step 2: Identify our own data source aliases
            var myDataSourceAliases = new HashSet<Guid>(this.dataSources.Select(x => x.Alias));

            // Step 3: Collect already projected data source columns
            var alreadyProjectedColumns = new HashSet<SqlDataSourceColumnExpression>(
                this.selectList
                    .Select(x => x.ColumnExpression)
                    .OfType<SqlDataSourceColumnExpression>()
            );

            // Step 4: Track our own data source's columns used in sub-queries
            var newEntries = new HashSet<SqlDataSourceColumnExpression>();
            foreach (var selectItem in this.selectList)
            {
                if (selectItem.ColumnExpression is SqlQueryableExpression subQuery)
                {
                    var usedColumns = DataSourceColumnUsageExtractor
                        .FindDataSources(myDataSourceAliases)
                        .In(subQuery)
                        .ExtractDataSourceColumnExpressions();

                    foreach (var column in usedColumns)
                    {
                        if (!alreadyProjectedColumns.Contains(column))
                            newEntries.Add(column);
                    }
                }
            }

            // Step 5: Append missing columns to select list
            int colIndex = 1;
            foreach (var col in newEntries)
            {
                string alias = $"SubQueryCol{colIndex++}";
                var selectItem = new SelectColumn(col, alias, scalarColumn: false);
                this.selectList.Add(selectItem);
            }
        }

        private SqlExpression[] WrapQueryIfCteReferencesExist(SqlExpression[] sqlExpressions)
        {
            if (sqlExpressions is null)
                throw new ArgumentNullException(nameof(sqlExpressions));

            var localDataSources = this.dataSources.Select(x => x.Alias).ToArray();
            // we are passing local data sources because CteDataSourceExtractor not only extracts the CTE data sources
            // from `sqlExpression` but also will find the given local data sources in the `sqlExpression` so that
            // we know that local data sources were referenced in `sqlExpression`

            // after below visit sqlExpression may modify because CteDataSourceExtractor will remove the CTE
            // from sqlExpression while extracting them in CteDataSources list
            List<SqlAliasedCteSourceExpression> extractedCteDataSources = new List<SqlAliasedCteSourceExpression>();
            HashSet<Guid> outerDataSourceUsed = new HashSet<Guid>();
            for (var i = 0; i < sqlExpressions.Length; i++)
            {
                var cteDataSourceExtractor = new CteReferenceAnalyzer(localDataSources);
                sqlExpressions[i] = cteDataSourceExtractor.Visit(sqlExpressions[i]);
                extractedCteDataSources.AddRange(cteDataSourceExtractor.CteDataSources);
                foreach (var outerDataSource in cteDataSourceExtractor.OuterDataSourcesUsed)
                    outerDataSourceUsed.Add(outerDataSource);
            }

            // if we find CTE data sources in `sqlExpression` it means it contains recursive CTE,
            // as we have already removed them from `sqlExpression` and extracted them, if below
            // condition is true then we'll proceed further to add them in this SqlSelectExpression
            if (extractedCteDataSources.Count > 0)
            {
                // IMPORTANT: we have extracted the external CTE data sources and we'll add them in 
                // this SqlSelectExpression but we don't have to mark this instance as CTE because
                // it will become CTE automatically once external CTE data sources are added,
                // however, we need to move the data sources of this SqlSelectExpression to it's local
                // CTE data source if one of local non-cte data sources were used in external CTE data source

                var localNonCteDataSources = this.dataSources.Where(x => !(x.QuerySource is SqlCteReferenceExpression));
                var anyLocalNonCteDataSourceReferenceIsGivenSqlExpression = localNonCteDataSources.Any(localCteDataSource => outerDataSourceUsed.Contains(localCteDataSource.Alias));
                if (anyLocalNonCteDataSourceReferenceIsGivenSqlExpression)
                {
                    // First we need to convert the whole query into CTE and while converting
                    // make sure we apply all the projections when converting below to derived table.

                    // As we found that local non-cte data source was used in the `sqlExpression` so we need to
                    // move current SqlSelectExpression to CTE
                    this.ApplyAutoProjectionIfPossible(applyAll: true);
                    var thisQueryToDeriveTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(this);

                    var fromSource = new SqlCteReferenceExpression(Guid.NewGuid());

                    this.InitializeCteQuery(fromSource, thisQueryToDeriveTable);

                    // it means local non-cte data source was used in the `sqlExpression`
                    sqlExpressions = this.ReplaceDataSourceAccessing(sqlExpressions);
                    for (var i = 0; i < extractedCteDataSources.Count; i++)
                    {
                        // IMPORTANT: below we are passing the extractedCteDataSources[i] instead of extractedCteDataSources[i].CteBody
                        // this is because the replacement system checks if it's inside a CTE data source that's when it puts adds the
                        // cross join otherwise not.
                        var sqlAliasedCteDataSource = this.ReplaceDataSourceAccessingSingle(extractedCteDataSources[i] /*do NOT pass .CteBody*/) as SqlAliasedCteSourceExpression
                                                    ??
                                                    throw new InvalidOperationException($"Expected expression type is '{nameof(SqlAliasedCteSourceExpression)}'.");
                        var querySourceUpdated = sqlAliasedCteDataSource.CteBody;
                        if (querySourceUpdated != extractedCteDataSources[i].CteBody)
                        {
                            extractedCteDataSources[i] = new SqlAliasedCteSourceExpression(querySourceUpdated, extractedCteDataSources[i].CteAlias);
                        }
                    }
                }

                this.cteDataSources.AddRange(extractedCteDataSources.Select(x => new CteDataSource(x.CteBody, x.CteAlias)));
            }

            return sqlExpressions;
        }

        private class CteReferenceAnalyzer : SqlExpressionVisitor
        {
            private readonly List<SqlAliasedCteSourceExpression> cteDataSources = new List<SqlAliasedCteSourceExpression>();
            private readonly HashSet<Guid> outerQueryDataSources;
            private readonly HashSet<Guid> outerDataSourcesUsed = new HashSet<Guid>();
            private readonly Stack<bool> cteDataSourceVisiting = new Stack<bool>();

            public IReadOnlyCollection<SqlAliasedCteSourceExpression> CteDataSources => this.cteDataSources;
            public IReadOnlyCollection<Guid> OuterDataSourcesUsed => this.outerDataSourcesUsed;

            public CteReferenceAnalyzer(Guid[] outerQueryDataSources)
            {
                this.outerQueryDataSources = new HashSet<Guid>(outerQueryDataSources) ?? throw new ArgumentNullException(nameof(outerQueryDataSources));
            }

            public override SqlExpression Visit(SqlExpression node)
            {
                return base.Visit(node);
            }

            protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
            {
                if (this.cteDataSourceVisiting.Count > 0 && this.cteDataSourceVisiting.Peek())
                {
                    if (this.outerQueryDataSources.Contains(node.DataSourceAlias))
                        this.outerDataSourcesUsed.Add(node.DataSourceAlias);
                }
                return base.VisitSqlDataSourceColumn(node);
            }

            protected internal override SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
            {
                var visitedNode = base.VisitSqlDerivedTable(node);
                if (node.IsCte)
                {
                    this.cteDataSources.AddRange(node.CteDataSources);
                    visitedNode = node.Update(null, node.FromSource, node.Joins, node.WhereClause, node.GroupByClause, node.HavingClause, node.OrderByClause, node.SelectColumnCollection);
                }
                return visitedNode;
            }

            protected internal override SqlExpression VisitSqlAliasedCteSource(SqlAliasedCteSourceExpression node)
            {
                this.cteDataSourceVisiting.Push(true);
                var visitedNode = base.VisitSqlAliasedCteSource(node);
                this.cteDataSourceVisiting.Pop();
                return visitedNode;
            }
        }

        // This class is only intended to be used on the SqlSelectExpression's constructor
        // when we receive the Sql Sub-Queries / Tables to create the SqlSelectExpression.
        // It adds the given sub-query as join in the given `selectQuery` while in normal
        // cases we don't add the data source as joined because it usually has already
        // been added
        private class QueryShapeComposer
        {
            private readonly SqlSelectExpression selectQuery;

            public QueryShapeComposer(SqlSelectExpression selectQuery)
            {
                this.selectQuery = selectQuery;
            }

            public SqlQueryShapeExpression ComposeQueryShape(SqlExpression sqlExpression)
            {
                if (sqlExpression is SqlMemberInitExpression queryShape)
                {
                    return this.ComposeQueryShapeByMemberInit(queryShape);
                }
                else if (sqlExpression is SqlQuerySourceExpression querySource)
                {
                    return this.ComposeQueryShapeByQuerySource(querySource);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot compose query shape for '{sqlExpression.GetType().Name}' expression.");
                }
            }

            private SqlMemberInitExpression ComposeQueryShapeByMemberInit(SqlMemberInitExpression queryShape)
            {
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));
                var expandedBindings = new List<SqlMemberAssignment>();
                foreach (var binding in queryShape.Bindings)
                {
                    SqlExpression memberValue = this.ComposeQueryShape(binding.SqlExpression);
                    var memberAssignment = new SqlMemberAssignment(binding.MemberName, memberValue);
                    expandedBindings.Add(memberAssignment);
                }
                return new SqlMemberInitExpression(expandedBindings);
            }

            private SqlDataSourceQueryShapeExpression ComposeQueryShapeByQuerySource(SqlQuerySourceExpression dataSource)
            {
                if (dataSource is null)
                    throw new ArgumentNullException(nameof(dataSource));

                if (dataSource is SqlDerivedTableExpression derivedTable)
                {
                    derivedTable = this.ExtractCteSourcesFromDerivedTableIfAvailable(derivedTable);
                    dataSource = derivedTable.ConvertToTableIfPossible();
                }

                var dataSourceAlias = Guid.NewGuid();
                AliasedDataSource aliasedDataSource;
                var firstEntry = this.selectQuery.dataSources.Count == 0;
                if (firstEntry)
                    aliasedDataSource = new AliasedDataSource(dataSource, dataSourceAlias);
                else
                    aliasedDataSource = new JoinDataSource(SqlJoinType.Cross, dataSource, dataSourceAlias, joinCondition: null, joinName: null, isNavigationJoin: false);
                this.selectQuery.AddAliasedDataSource(aliasedDataSource);
                return this.selectQuery.CreateDataSourceQueryShape(aliasedDataSource);
            }

            private SqlDerivedTableExpression ExtractCteSourcesFromDerivedTableIfAvailable(SqlDerivedTableExpression derivedTableSource)
            {
                if (derivedTableSource is null)
                    throw new ArgumentNullException(nameof(derivedTableSource));
                if (derivedTableSource.CteDataSources.Length > 0)
                {
                    var cteDsList = derivedTableSource.CteDataSources;
                    foreach (var cteDs in cteDsList)
                    {
                        this.selectQuery.cteDataSources.Add(new CteDataSource(cteDs.CteBody, cteDs.CteAlias));
                    }
                    var d = derivedTableSource;
                    derivedTableSource = derivedTableSource.Update(null, d.FromSource, d.Joins, d.WhereClause, d.GroupByClause, d.HavingClause, d.OrderByClause, d.SelectColumnCollection);
                }
                return derivedTableSource;
            }
        }
    }
}
