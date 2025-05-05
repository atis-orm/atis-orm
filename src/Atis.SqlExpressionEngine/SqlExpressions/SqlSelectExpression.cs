using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Exceptions;
using Atis.SqlExpressionEngine.Internal;
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
    public partial class SqlSelectExpression : SqlDataSourceReferenceExpression
    {
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Select;

        private readonly List<AliasedDataSource> dataSources = new List<AliasedDataSource>();
        public IReadOnlyCollection<AliasedDataSource> DataSources => dataSources;
        private readonly List<CteDataSource> cteDataSources = new List<CteDataSource>();
        public IReadOnlyCollection<CteDataSource> CteDataSources => cteDataSources;
        private readonly List<SelectColumn> selectList = new List<SelectColumn>();
        public IReadOnlyCollection<SelectColumn> SelectList => selectList;
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
        public ISqlExpressionFactory SqlFactory { get; }
        public bool AutoProjection { get; private set; }
        public string Tag { get; set; }

        private readonly SqlQueryModelBinding modelBinding = new SqlQueryModelBinding();
        private readonly Dictionary<Guid, SqlQueryModelBinding> dataSourceModelBinding = new Dictionary<Guid, SqlQueryModelBinding>();

        public SqlSelectExpression(CteDataSource[] cteDataSources, SqlTableExpression table, ISqlExpressionFactory sqlFactory)
            : this(cteDataSources, 
                    (sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory))).CreateCompositeBindingForSingleExpression(table, ModelPath.Empty), 
                    sqlFactory)
        {
        }

        public SqlSelectExpression(CteDataSource[] cteDataSources, SqlQuerySourceExpression querySource, ISqlExpressionFactory sqlFactory)
            : this(cteDataSources, 
                    (sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory))).
                        CreateCompositeBindingForSingleExpression(querySource ?? throw new ArgumentNullException(nameof(querySource)), ModelPath.Empty), 
                    sqlFactory)
        {
        }

        public SqlSelectExpression(CteDataSource[] cteDataSources, SqlCompositeBindingExpression compositeBinding, ISqlExpressionFactory sqlFactory)
        {
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));

            if (compositeBinding is null)
                throw new ArgumentNullException(nameof(compositeBinding));

            if (compositeBinding.Bindings.Length > 1 && compositeBinding.Bindings.Any(x => x.ModelPath.IsEmpty))
                throw new ArgumentException($"There are more than 1 items in the {nameof(compositeBinding)}, but one of them has empty model path.", nameof(compositeBinding));

            if (cteDataSources?.Length > 0)
            {
                this.cteDataSources.AddRange(cteDataSources);
            }

            for (var i = 0; i < compositeBinding.Bindings.Length; i++)
            {
                var binding = compositeBinding.Bindings[i];
                // get the query source (Derived Table, Table, CTE, etc.)
                var querySource = binding.SqlExpression as SqlQuerySourceExpression
                                            ??
                                            throw new ArgumentException($"Binding expression '{binding.SqlExpression}' is not a valid query source expression.", nameof(binding));
                if (querySource is SqlDerivedTableExpression derivedTable &&
                    derivedTable.CteDataSources.Length > 0)
                {
                    // extracting CTE Data Sources from derived table
                    this.cteDataSources.AddRange(derivedTable.CteDataSources.Select(x => new CteDataSource(x.CteBody, x.CteAlias)));
                    // removing CTE Data Sources from derived table
                    querySource = derivedTable.Update(null, derivedTable.FromSource, derivedTable.Joins, derivedTable.WhereClause, derivedTable.GroupByClause, derivedTable.HavingClause, derivedTable.OrderByClause, derivedTable.SelectColumnCollection)
                                                .ConvertToTableIfPossible();
                }

                AliasedDataSource aliasedDataSource;
                if (i > 0)
                    // if this is not 1st data source then it means this will be added as cross join
                    aliasedDataSource = new JoinDataSource(SqlJoinType.Cross, querySource, Guid.NewGuid(), joinCondition: null, joinName: null, isNavigationJoin: false, navigationParent: null);
                else
                    // so first data source will be added directly in data source
                    aliasedDataSource = new AliasedDataSource(querySource, Guid.NewGuid());

                // now we are creating model binding for the data source, this is important because 
                // if a data source is provided to resolve a model path we will pick the data source's model binding
                this.AddAliasedDataSource(aliasedDataSource);

                if (!binding.ModelPath.IsEmpty)
                {
                    // if ModelPath is not empty then it means the projection of data source's will not
                    // be added into this query's modelBinding list, rather we'll only add the data sources
                    // in query's modelBinding list
                    this.modelBinding.AddBinding(this.CreateDataSource(aliasedDataSource.Alias), binding.ModelPath);
                }
                else
                {
                    // if binding.ModelPath is empty then this method will be called with only 1 data source as default
                    // e.g. students.Where(x => x.StudentId == "123");
                    // `students` will be converted to SqlSelectExpression with single data source without a ModelPath
                    // that's where we will add all the student columns in current Select Expression's binding list
                    // which is modelBinding
                    AddQuerySourceColumnsInQueryModelBinding(aliasedDataSource, this.modelBinding, binding.ModelPath);
                }
            }

            this.SqlFactory = sqlFactory;
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

        public void ApplyProjection(SqlCompositeBindingExpression boundSelectList)
        {
            if (boundSelectList is null)
                throw new ArgumentNullException(nameof(boundSelectList));
            if (boundSelectList.Bindings.Length == 0)
                throw new ArgumentException("Select list must have at least one binding.", nameof(boundSelectList));
            if (boundSelectList.Bindings.Length > 1 && boundSelectList.Bindings.Any(x => x.ModelPath.IsEmpty))
                throw new ArgumentException($"There are more than 1 items in the {nameof(boundSelectList)}, but one of them has empty model path.", nameof(boundSelectList));

            boundSelectList = this.WrapIfRequiredSingle(SqlQueryOperation.Select, boundSelectList) as SqlCompositeBindingExpression
                                ??
                                throw new InvalidOperationException($"Select list '{boundSelectList}' is not a valid select list.");

            if (this.selectList.Count > 0)
                throw new InvalidOperationException("Select list already has been defined.");

            boundSelectList = this.ConvertDerivedTableToOuterApplyProjections(boundSelectList);

            this.UpdateModelBinding(boundSelectList);
            this.ApplyProjectionFromModelBinding(applyAll: false);
        }

        private SqlCompositeBindingExpression ConvertDerivedTableToOuterApplyProjections(SqlCompositeBindingExpression boundSelectList)
        {
            var bindingToRemove = new List<SqlExpressionBinding>();
            var bindingsToAdd = new List<SqlExpressionBinding>();
            var bindingChanged = false;
            foreach (var binding in boundSelectList.Bindings)
            {
                bool addBinding = true;
                if (binding.SqlExpression is SqlDerivedTableExpression derivedTable)
                {
                    if (this.ShouldBeAddedAsOuterApply(derivedTable))
                    {
                        var dataSource = this.AddNavigationJoin(this, derivedTable, SqlJoinType.OuterApply, binding.ModelPath, binding.ModelPath.GetLastElementRequired());
                        bindingChanged = true;
                        addBinding = false;
                        var projections = derivedTable.GetColumnModelMap();
                        foreach (var projection in projections)
                        {
                            var dsColumn = new SqlDataSourceColumnExpression(dataSource.DataSourceAlias, projection.ColumnName);
                            bindingsToAdd.Add(new SqlExpressionBinding(dsColumn, binding.ModelPath.Append(projection.ModelPath)));
                        }
                    }
                }
                if (addBinding)
                    bindingsToAdd.Add(binding);
            }
            if (bindingChanged)
            {
                boundSelectList = new SqlCompositeBindingExpression(bindingsToAdd.ToArray());
            }

            return boundSelectList;
        }

        private bool ShouldBeAddedAsOuterApply(SqlDerivedTableExpression derivedTable)
        {
            if (derivedTable.NodeType == SqlExpressionType.DataManipulationDerivedTasble)
                return false;
            //var myDataSources = new HashSet<Guid>(this.dataSources.Select(x => x.Alias));
            //var myColumnsUsedInDerivedTable = DataSourceColumnUsageExtractor.FindDataSources(myDataSources).In(derivedTable).ExtractDataSourceColumnExpressions();
            if (derivedTable.SelectColumnCollection?.SelectColumns.All(x => x.ScalarColumn) ?? false)
                // if all are scalar columns then cannot be outer apply
                return false;
            else
                // if there are no scalar columns then it must be outer apply
                return true;
            //if (myColumnsUsedInDerivedTable.Count > 0)
            //    return true;
            //return false;
        }

        public bool ApplyAutoProjection()
        {
            return this.ApplyAutoProjectionIfPossible(applyAll: false);
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

        public SqlDataSourceExpression AddJoin(SqlQuerySourceExpression querySource, SqlJoinType joinType)
        {
            return this.AddJoinedSource(SqlQueryOperation.Join, this, querySource, joinType, navigationPath: null);
        }

        public bool TryResolveNavigationDataSource(SqlDataSourceReferenceExpression navigationParent, ModelPath modelPath, out SqlDataSourceExpression navigationDataSource)
        {
            if (navigationParent.TryResolveExact(modelPath, out SqlExpression sqlExpression))
            {
                navigationDataSource = sqlExpression as SqlDataSourceExpression
                                        ??
                                        throw new InvalidOperationException($"Expected a {nameof(SqlDataSourceExpression)} but got {sqlExpression.GetType().Name}.");
                return true;
            }
            navigationDataSource = null;
            return false;
        }

        public SqlDataSourceExpression AddNavigationJoin(SqlDataSourceReferenceExpression navigationParent, SqlExpression joinedSource, SqlJoinType joinType, ModelPath navigationPath, string navigationName)
        {
            if (navigationParent is null)
                throw new ArgumentNullException(nameof(navigationParent));
            if (joinedSource is null)
                throw new ArgumentNullException(nameof(joinedSource));
            if (navigationPath.IsEmpty)
                throw new ArgumentException($"Navigation path cannot be empty.", nameof(navigationPath));
            
            if (this.TryResolveNavigationDataSource(navigationParent, navigationPath, out _))
                throw new InvalidOperationException($"Navigation join already exists for the given navigation path '{navigationPath}'.");

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
            var dataSource = this.AddJoinedSource(SqlQueryOperation.NavigationJoin, navigationParent, sqlQuerySource, joinType, navigationPath, joinName: navigationName);
            return dataSource;
        }

        // TODO: check if we can move isDefaultEmpty logic inside here as well
        public SqlDataSourceExpression AddDataSourceWithJoinResolution(SqlQuerySourceExpression newQuerySource, bool isDefaultIfEmpty)
        {
            // This method is called through SelectMany converter where SelectMany has received an external query
            // and it is sending that external query to this method.
            SqlJoinType joinType = SqlJoinType.Cross;
            SqlExpression joinCondition = null;
            Guid? newDataSourceAlias = null;
            string tag = null;
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
                    tag = derivedTable.Tag;
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
            var dataSource = this.AddJoinedSource(newOperation, this, newQuerySource, joinType, navigationPath: null, dataSourceAlias: newDataSourceAlias, joinCondition: joinCondition, joinName: tag);

            return dataSource;
        }

        public void AddCteDataSource(SqlSubQuerySourceExpression cteBody, Guid cteAlias)
        {
            if (this.cteDataSources.Any(x => x.CteAlias == cteAlias))
                throw new InvalidOperationException($"CTE with alias {cteAlias} already exists.");
            this.cteDataSources.Add(new CteDataSource(cteBody, cteAlias));
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

            foreach (var querySourceColumn in querySource.GetColumnModelMap())
            {
                var dsColumn = new SqlDataSourceColumnExpression(fromSource.Alias, querySourceColumn.ColumnName);
                this.modelBinding.AddBinding(dsColumn, querySourceColumn.ModelPath);
            }
        }

        public void SwitchBindingToLastDataSource()
        {
            var dataSource = this.dataSources.Last();
            this.modelBinding.Reset();
            var aliasedDataSource = this.dataSources.Where(x => x.Alias == dataSource.Alias).First();
            AddQuerySourceColumnsInQueryModelBinding(aliasedDataSource, this.modelBinding, ModelPath.Empty);
        }

        public void UpdateJoin(Guid joinDataSourceAlias, SqlJoinType joinType, SqlExpression joinCondition, string joinName, bool navigationJoin, Guid? navigationParent)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            this.dataSources[indexOfDataSource] = new JoinDataSource(joinType, dataSource.QuerySource, joinDataSourceAlias, joinCondition: joinCondition, joinName: joinName, isNavigationJoin: navigationJoin, navigationParent: navigationParent);
        }

        public void UpdateJoinType(Guid joinDataSourceAlias, SqlJoinType joinType)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                this.dataSources[indexOfDataSource] = new JoinDataSource(joinType, joinDataSource.QuerySource, joinDataSourceAlias, joinCondition: joinDataSource.JoinCondition, joinName: joinDataSource.JoinName, isNavigationJoin: joinDataSource.IsNavigationJoin, navigationParent: joinDataSource.NavigationParent);
            else
                throw new InvalidOperationException($"Join data source with alias {joinDataSourceAlias} is not a join data source.");
        }

        public void UpdateJoinCondition(Guid joinDataSourceAlias, SqlExpression joinCondition)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                this.dataSources[indexOfDataSource] = new JoinDataSource(joinDataSource.JoinType, joinDataSource.QuerySource, joinDataSource.Alias, joinCondition: joinCondition, joinName: joinDataSource.JoinName, isNavigationJoin: joinDataSource.IsNavigationJoin, navigationParent: joinDataSource.NavigationParent);
            else
                throw new InvalidOperationException($"Join data source with alias {joinDataSourceAlias} is not a join data source.");
        }

        public void UpdateJoinName(Guid joinDataSourceAlias, string joinName)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                this.dataSources[indexOfDataSource] = new JoinDataSource(joinDataSource.JoinType, joinDataSource.QuerySource, joinDataSource.Alias, joinCondition: joinDataSource.JoinCondition, joinName: joinName, isNavigationJoin: joinDataSource.IsNavigationJoin, navigationParent: joinDataSource.NavigationParent);
            else
                throw new InvalidOperationException($"Join data source with alias {joinDataSourceAlias} is not a join data source.");
        }

        public SqlJoinType GetJoinType(Guid joinDataSourceAlias)
        {
            var (dataSource, indexOfDataSource) = this.GetAliasedDataSourceRequired(joinDataSourceAlias);
            if (dataSource is JoinDataSource joinDataSource)
                return joinDataSource.JoinType;
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
                var autoProjectionWasApplied = this.ApplyAutoProjectionIfPossible(applyAll: false);

                this.WrapQuery(autoProjectionWasApplied);

                if (sqlExpressions?.Length > 0)
                {
                    sqlExpressions = this.ReplaceDataSourceAccessing(sqlExpressions);
                }
            }

            return sqlExpressions;
        }

        protected virtual void WrapQuery(bool autoProjectionWasApplied)
        {
            var currentCteDataSources = this.cteDataSources.ToArray();
            for (var i = 0; i < currentCteDataSources.Length; i++)
                this.cteDataSources.Remove(currentCteDataSources[i]);

            this.AppendSelectListWithColumnsUsedInSubQuery();

            // if auto projection was applied we'll keep the non projectable bindings,
            // that were added during GroupJoin / Let keyword
            var nonProjectableBindings = autoProjectionWasApplied ? this.modelBinding.GetNonProjectableBindings() : Array.Empty<SqlExpressionBinding>();

            // You might think that why not move the queryableSelectedInProject into autoProjectionWasApplied flag
            // but we cannot do that, read below flow:
            // 1. SqlQueryableExpression was applied in Select List of this SqlSelectExpression for the first time
            //      in above case it would NOT be present in modelBinding for now because it's just applied in Select
            // 2. Below will extract the SqlQueryableExpression from Select List and will Concat it in nonProjectableBindings
            //      this is because although it was not in non-projectable binding list but still it's in Select list
            //      therefore, it should be available in next method, without this in the modelBinding we will face error
            //      when user will try to access, for example dbc.Table.Select(x => new { .. G = queryable }).Where(x => x.G.Any(...))
            //      in this example, when user will do `x.G` it would try to resolve it and if it's not in modelBinding 
            //      this will not be resolved.
            // 3. Next user applied Where method and used this SqlQueryableExpression which renders as Exist
            // 4. Assume that next wrap happened with Auto Projection = false and user didn't select this SqlQueryableExpression,
            //      in that case nonProjectableBindings will be empty (autoProjectionWasApplied = true), thus SqlQueryableExpression will not be picked up,
            //      and below queryableSelectedInProjection will not have any elements, eventually at the end of this method
            //      when it's adding nonProjectableBindings there will be nothing in the array, so this SqlQueryableExpression
            //      will become out of scope

            var queryableSelectedInProjection = this.selectList.Where(x => x.ColumnExpression is SqlQueryableExpression).ToArray();
            if (queryableSelectedInProjection.Length > 0)
                nonProjectableBindings = nonProjectableBindings.Concat(queryableSelectedInProjection.Select(x => new SqlExpressionBinding(x.ColumnExpression, x.ModelPath))).ToArray();

            foreach (var queryable in queryableSelectedInProjection)
                this.selectList.Remove(queryable);

            var derivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(this);

            this.Initialize();
            
            var derivedTableDataSource = new AliasedDataSource(derivedTable, Guid.NewGuid());
            AddAliasedDataSource(derivedTableDataSource);

            for (var i = 0; i < currentCteDataSources.Length; i++)
                this.cteDataSources.Add(currentCteDataSources[i]);

            AddQuerySourceColumnsInQueryModelBinding(derivedTableDataSource, this.modelBinding, ModelPath.Empty);

            // Lift Up: adding back because they were removed during Initialize
            if (nonProjectableBindings.Length > 0)
            {
                // When adding back we are replacing the data source columns of this query with
                // new sub-query columns in those expressions as well. Note that this happens
                // in the WrapIfRequired method for the given SqlExpression[] but these
                // expressions also need to be replaced as they are lifting up
                var innerExpressions = nonProjectableBindings.Select(x => x.SqlExpression).ToArray();
                innerExpressions = this.ReplaceDataSourceAccessing(innerExpressions);
                for (var i = 0; i < innerExpressions.Length; i++)
                {
                    nonProjectableBindings[i] = nonProjectableBindings[i].UpdateExpression(innerExpressions[i]);
                }

                this.modelBinding.AddBindings(nonProjectableBindings);
            }
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
                // then we remove all the navigation data sources from modelBinding so that it
                // will cause the joining to happen again
                var autoNavigationDataSourcesInBinding = this.modelBinding.GetFilteredByExpression(this.IsAutoJoinedDataSource);
                if (autoNavigationDataSourcesInBinding.Length > 0)
                {
                    foreach (var autoNavigationDataSource in autoNavigationDataSourcesInBinding)
                    {
                        this.modelBinding.Remove(autoNavigationDataSource);
                    }
                }
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
            this.modelBinding.Reset();
            this.dataSourceModelBinding.Clear();
        }

        public void UndoProjection()
        {
            this.selectList.Clear();
        }

        public void MarkModelBindingAsNonProjectable(ModelPath modelPath)
        {
            this.modelBinding.MarkBindingAsNonProjectable(modelPath);
        }

        public void UpdateModelBinding(SqlCompositeBindingExpression compositeBinding)
        {
            if (compositeBinding is null)
                throw new ArgumentNullException(nameof(compositeBinding));

            var currentBindings = this.modelBinding.CreateCopy();
            var firstDataSource = this.dataSources.First();
            var oneDataSource = currentBindings.All(x => (x.SqlExpression as SqlDataSourceColumnExpression)?.DataSourceAlias == firstDataSource.Alias && !x.ModelPath.IsEmpty);

            this.modelBinding.Reset();

            foreach (var binding in compositeBinding.Bindings)
            {
                if (binding.SqlExpression == this)
                {
                    if (oneDataSource)
                    {
                        var ds = this.CreateDataSource(firstDataSource.Alias);
                        this.modelBinding.AddBinding(ds, binding.ModelPath);
                    }
                    else
                    {
                        var newBindings = currentBindings.Select(x => x.PrependPath(binding.ModelPath)).ToArray();
                        this.modelBinding.AddBindings(newBindings);
                    }
                }
                else
                {
                    // if the binding is not a data source or not in the temp data source model binding dictionary
                    // then we can add it to the main model binding
                    this.modelBinding.AddBinding(binding.SqlExpression, binding.ModelPath);
                }
            }
        }

        /// <inheritdoc />
        public override bool TryResolveScalarColumn(out SqlExpression scalarColumnExpression)
        {
            var firstSelectItem = this.selectList.FirstOrDefault();
            if (firstSelectItem?.ScalarColumn == true)
            {
                scalarColumnExpression = firstSelectItem.ColumnExpression;
                return true;
            }
            if (this.dataSources.Count == 1)
            {
                var ds0Alias = this.dataSources[0].Alias;
                if (this.dataSourceModelBinding.ContainsKey(ds0Alias) && this.TryResolveScalarColumnByDataSourceAlias(ds0Alias, out var scalarCol))
                {
                    scalarColumnExpression = scalarCol;
                    return true;
                }
            }
            scalarColumnExpression = null;
            return false;
        }

        public bool TryResolveScalarColumnByDataSourceAlias(Guid dataSourceAlias, out SqlExpression scalarColumnExpression)
        {
            var dataSource = this.dataSources.Where(x => x.Alias == dataSourceAlias).FirstOrDefault()
                            ??
                            throw new ArgumentException($"Data source with alias '{dataSourceAlias}' not found.", nameof(dataSourceAlias));
            if (!this.dataSourceModelBinding.TryGetValue(dataSourceAlias, out var queryModelBinding))
                throw new ArgumentException($"Data source with alias '{dataSourceAlias}' not found.", nameof(dataSourceAlias));
            return queryModelBinding.TryResolveExact(ModelPath.Empty, out scalarColumnExpression);
        }

        /// <inheritdoc />
        public override SqlExpression Resolve(ModelPath modelPath)
        {
            if (this.selectList.Count > 0)
            {
                return this.ResolveBySelect(modelPath);
            }

            return ResolveCore(this.modelBinding, modelPath);
        }

        /// <inheritdoc />
        public override bool TryResolveExact(ModelPath modelPath, out SqlExpression resolvedExpression)
        {
            if (this.selectList.Count > 0)
            {
                return this.TryResolveBySelectExact(modelPath, out resolvedExpression);
            }
            return this.modelBinding.TryResolveExact(modelPath, out resolvedExpression);
        }

        public SqlExpression ResolveByDataSourceAlias(Guid dataSourceAlias, ModelPath modelPath)
        {
            if (!this.dataSourceModelBinding.TryGetValue(dataSourceAlias, out var queryModelBinding))
                throw new ArgumentException($"Data source with alias '{dataSourceAlias}' not found, modelPath = '{modelPath}'.", nameof(dataSourceAlias));

            return ResolveCore(queryModelBinding, modelPath, dataSourceAlias);
        }

        public bool TryResolveExactByDataSourceAlias(Guid dataSourceAlias, ModelPath modelPath, out SqlExpression resolvedExpression)
        {
            if (!this.dataSourceModelBinding.TryGetValue(dataSourceAlias, out var queryModelBinding))
                throw new ArgumentException($"Data source with alias '{dataSourceAlias}' not found.", nameof(dataSourceAlias));
            return queryModelBinding.TryResolveExact(modelPath, out resolvedExpression);
        }

        public SqlExpression ResolveGroupBy(ModelPath modelPath)
        {
            if (this.TryResolveGroupByExact(modelPath, out var groupByExpression))
            {
                return groupByExpression;
            }

            var compositeBinding = this.ResolveGroupByPartial(modelPath);

            return CreateCompositeBindingExpressionWithPathRemoved(compositeBinding.Bindings, pathToRemove: modelPath);
        }





        private void AddQuerySourceColumnsInQueryModelBinding(AliasedDataSource aliasedDataSource, SqlQueryModelBinding queryModelBinding, ModelPath dataSourceModelPath)
        {
            // when adding model binding in given queryModelBinding instance we usually
            // create SqlDataSourceColumnExpression, however, in-case if the given binding
            // contains QueryableColumnModelPath it means it has the Queryable Derived Table
            // selected, so we'll set it as is, that is, we will not create SqlDataSourceColumnExpression
            // for that binding
            HashSet<ColumnModelPath> dataSourceColumns;
            if (aliasedDataSource.QuerySource is SqlCteReferenceExpression cteRef)
            {
                var ds = this.cteDataSources.Where(x => x.CteAlias == cteRef.CteAlias).FirstOrDefault()
                        ??
                        throw new ArgumentException($"CTE Data source with alias '{cteRef.CteAlias}' not found.", nameof(aliasedDataSource));
                dataSourceColumns = ds.CteBody.GetColumnModelMap();
            }
            else
                dataSourceColumns = aliasedDataSource.QuerySource.GetColumnModelMap();
            foreach (var querySourceColumn in dataSourceColumns)
            {
                SqlExpression expressionToAdd;
                bool nonProjectable = false;
                if (querySourceColumn is QueryableColumnModelPath queryableModelPath)
                {
                    expressionToAdd = queryableModelPath.Queryable;
                    nonProjectable = true;
                }
                else
                {
                    expressionToAdd = new SqlDataSourceColumnExpression(aliasedDataSource.Alias, querySourceColumn.ColumnName);
                }
                queryModelBinding.AddBinding(expressionToAdd, dataSourceModelPath.Append(querySourceColumn.ModelPath), nonProjectable: nonProjectable);
            }
        }
        private SqlCompositeBindingExpression CreateCompositeBindingExpressionWithPathRemoved(SqlExpressionBinding[] bindings, ModelPath pathToRemove)
        {
            var newBindings = bindings
                                    .Select(binding => new SqlExpressionBinding(binding.SqlExpression, binding.ModelPath.RemoveFromLeft(pathToRemove)))
                                    .ToArray();
            return this.SqlFactory.CreateCompositeBindingForMultipleExpressions(newBindings);
        }

        private bool TryResolveBySelectExact(ModelPath modelPath, out SqlExpression resolvedExpression)
        {
            var item = this.selectList.Where(x => x.ModelPath.Equals(modelPath)).FirstOrDefault();
            if (item != null)
            {
                resolvedExpression = item.ColumnExpression;
                return true;
            }
            resolvedExpression = null;
            return false;
        }

        private SqlExpression ResolveBySelect(ModelPath modelPath)
        {
            SqlExpression result;
            if (this.TryResolveBySelectExact(modelPath, out var resolvedExpression))
            {
                result = resolvedExpression;
            }
            else
            {
                var items = this.selectList.Where(x => x.ModelPath.StartsWith(modelPath))
                                           .Select(x => new SqlExpressionBinding(x.ColumnExpression, x.ModelPath))
                                           .ToArray();
                if (items.Length == 0)
                    throw new UnresolvedMemberAccessException(modelPath);
                result = new SqlCompositeBindingExpression(items);
            }
            return result;
        }

        private SqlExpression ResolveCore(SqlQueryModelBinding modelBinding, ModelPath modelPath, Guid? dataSourceAlias = null)
        {
            SqlExpression result;
            if (modelPath.IsEmpty)
                throw new ArgumentException($"Model path cannot be empty.", nameof(modelPath));

            if (modelBinding.TryResolveExact(modelPath, out var sqlExpression))
            {
                result = sqlExpression;
            }
            else
            {
                var bindings = modelBinding.ResolvePartial(modelPath);
                if (bindings.Length == 0)
                {
                    string errorForDataSource = dataSourceAlias.HasValue ? $" for data source with alias '{dataSourceAlias}'" : string.Empty;
                    throw new InvalidOperationException($"No binding was found for path '{modelPath}'{errorForDataSource}");
                }
                var compositeBinding = CreateCompositeBindingExpressionWithPathRemoved(bindings, pathToRemove: modelPath);
                result = compositeBinding;
            }

            if (result is SqlDerivedTableExpression derivedTable)
            {
                this.modelBinding.RemoveByPath(modelPath);
                var dataSource = this.AddNavigationJoin(this, derivedTable, SqlJoinType.CrossApply, modelPath, modelPath.GetLastElementRequired());
                var projections = derivedTable.GetColumnModelMap();
                foreach (var projection in projections)
                {
                    var dsColumn = new SqlDataSourceColumnExpression(dataSource.DataSourceAlias, projection.ColumnName);
                    this.modelBinding.AddBinding(dsColumn, modelPath.Append(projection.ModelPath));
                }
                result = dataSource;
            }
            else if (result is SqlQueryableExpression queryable)
            {
                result = queryable.Query;
            }

            return result;
        }

        private bool TryResolveGroupByExact(ModelPath modelPath, out SqlExpression groupByExpression)
        {
            // Case-1: GroupBy is multiple fields       new { C1 = x.Col1, C2 = x.Col2 }
            //  Case-1-1: modelPath = Empty             x.Key
            //      Result = this method will return false
            //  Case-1-2: modelPath = is not empty      x.Key.Col1
            //      Result = this method will return true if modelPath matches with exact binding
            // Case-2: GroupBy is single field          .GroupBy(x => x.Col1)
            //  Case-2-1: modelPath = Empty             x.Key
            //      Result = this method will return true and expression
            //  Case-2-2: modelPath = is not empty      x.Key.Col1
            //      Result = this method will throw exception

            // modelPath can be empty
            if (this.GroupByClause is SqlCompositeBindingExpression compositeBinding)
            {
                var binding = compositeBinding.Bindings.Where(x => x.ModelPath.Equals(modelPath)).FirstOrDefault();
                if (binding != null)
                {
                    groupByExpression = binding.SqlExpression;
                    return true;
                }
            }
            else
            {
                if (!modelPath.IsEmpty)
                    throw new InvalidOperationException($"Grouping was performed on single column, but model path '{modelPath}' was provided.");
                if (this.GroupByClause is null)
                    throw new InvalidOperationException($"GroupByClause is null.");
                groupByExpression = this.GroupByClause;
                return true;
            }
            groupByExpression = null;
            return false;
        }

        private SqlCompositeBindingExpression ResolveGroupByPartial(ModelPath modelPath)
        {
            if (this.GroupByClause is SqlCompositeBindingExpression compositeBinding)
            {
                var bindings = compositeBinding.Bindings.Where(x => x.ModelPath.StartsWith(modelPath)).ToArray();
                if (bindings.Length == 0)
                    throw new InvalidOperationException($"No grouping was found for model path '{modelPath}'.");
                return this.SqlFactory.CreateCompositeBindingForMultipleExpressions(bindings);
            }

            throw new InvalidOperationException($"Grouping was performed on single column cannot use Resolve Partial.");
        }

        private SqlDataSourceExpression AddJoinedSource(SqlQueryOperation newOperation, SqlDataSourceReferenceExpression parentSource, SqlQuerySourceExpression querySource, SqlJoinType joinType, ModelPath? navigationPath, Guid? dataSourceAlias = null, SqlExpression joinCondition = null, string joinName = null)
        {
            if (parentSource == null)
                throw new ArgumentNullException(nameof(parentSource));
            if (querySource is null)
                throw new ArgumentNullException(nameof(querySource));

            // It is important that we pass `NavigationJoin` as the next operation instead of `Join` when `navigationPath`
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

            var aliasedDataSource = new JoinDataSource(joinType, querySource, dataSourceAlias ?? Guid.NewGuid(), joinCondition: joinCondition, joinName: joinName, isNavigationJoin: newOperation == SqlQueryOperation.NavigationJoin, navigationParent: null);            
            // here we will initialize new QueryModelBinding for the aliased data source and
            // add the data source's columns in it
            this.AddAliasedDataSource(aliasedDataSource);

            // this variable will be used to set in the AddBinding method if navigationPath is given
            var dataSource = this.CreateDataSource(aliasedDataSource.Alias);

            if (navigationPath != null)
            {
                SqlQueryModelBinding queryModelBinding;
                if (parentSource == this)
                {
                    queryModelBinding = this.modelBinding;
                }
                else if (parentSource is SqlDataSourceExpression ds)
                {
                    if (!this.dataSourceModelBinding.TryGetValue(ds.DataSourceAlias, out queryModelBinding))
                        throw new InvalidOperationException($"No Model Binding was found for Data Source '{ds.DataSourceAlias}'");
                }
                else
                    throw new InvalidOperationException($"Argument '{nameof(parentSource)}' is neither data source nor current select expression.");

                queryModelBinding.AddBinding(dataSource, navigationPath.Value, nonProjectable: newOperation == SqlQueryOperation.NavigationJoin);
            }

            this.OnAfterApply(newOperation);

            return dataSource;
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

        private string GenerateUniqueColumnAlias(Dictionary<string, SelectColumn> columns, string columnAlias)
        {
            int i = 1;
            var newColumnAlias = $"{columnAlias}_{i}";
            while (columns.ContainsKey(newColumnAlias))
            {
                i++;
                newColumnAlias = $"{columnAlias}_{i}";
            }
            return newColumnAlias;
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

            var dataSourceModelBinding = new SqlQueryModelBinding();
            this.dataSourceModelBinding.Add(aliasedDataSource.Alias, dataSourceModelBinding);
            // here we are filling the data source's column list in `dataSourceModelBinding` with ModelPath.Empty because
            // if we want to access data source related binding we will provide relative path not full path
            AddQuerySourceColumnsInQueryModelBinding(aliasedDataSource, dataSourceModelBinding, ModelPath.Empty);
        }

        private SqlDataSourceExpression CreateDataSource(Guid dataSourceAlias)
        {
            return new SqlDataSourceExpression(this, dataSourceAlias);
        }

        private bool ApplyAutoProjectionIfPossible(bool applyAll)
        {
            if (this.selectList.Count > 0)
                return false;

            if (this.GroupByClause != null)
            {
                SqlExpressionBinding[] bindings;
                var groupBindingExpression = this.ResolveGroupBy(ModelPath.Empty);
                if (groupBindingExpression is SqlCompositeBindingExpression groupBindings)
                {
                    bindings = groupBindings.Bindings;
                }
                else
                {
                    bindings = new[] { new SqlExpressionBinding(groupBindingExpression, ModelPath.Empty) };
                }
                this.modelBinding.Reset();
                this.modelBinding.AddBindings(bindings);
            }
            this.ApplyProjectionFromModelBinding(applyAll: applyAll);
            this.AutoProjection = true;
            return true;
        }

        private void ApplyProjectionFromModelBinding(bool applyAll)
        {
            if (this.selectList.Count != 0)
                throw new InvalidOperationException($"Projection has already been applied");

            SqlExpressionBinding[] bindings;
            if (!applyAll)
                bindings = this.modelBinding.GetProjectableBindings();
            else
                bindings = this.modelBinding.CreateCopy();
                //bindings = bindings.Where(x => !IsAutoJoinedDataSource(x.SqlExpression)).ToArray();

            var projection = bindings;
            var expandedBindings = this.ExpandBindings(projection).ToList();

            var duplicationRemoved = this.FixDuplicateColumns(expandedBindings.ToArray());
            this.selectList.AddRange(duplicationRemoved);

            this.OnAfterApply(SqlQueryOperation.Select);
        }

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
                var binding = new SqlExpressionBinding(col, new ModelPath(alias));
                var selectItem = new SelectColumn(binding.SqlExpression, alias, binding.ModelPath, scalarColumn: false);
                this.selectList.Add(selectItem);
            }
        }


        private bool IsAutoJoinedDataSource(SqlExpression sqlExpression)
        {
            if (!(sqlExpression is SqlDataSourceExpression ds))
                return false;
            var aliasedDataSource = this.dataSources.Where(x => x.Alias == ds.DataSourceAlias).FirstOrDefault();
            if (aliasedDataSource == null)
                return false;
            if (!(aliasedDataSource is JoinDataSource joinDs))
                return false;
            return joinDs.IsNavigationJoin;
        }

        private SqlExpressionBinding[] ExpandBindings(SqlExpressionBinding[] bindings)
        {
            var expandedBindings = new List<SqlExpressionBinding>();
            foreach (var binding in bindings)
            {
                if (binding.SqlExpression is SqlDataSourceExpression ds)
                {
                    if (ds.SelectQuery != this)
                        throw new InvalidOperationException($"Data source '{ds.DataSourceAlias}' is not part of this select query.");
                    if (!this.dataSourceModelBinding.TryGetValue(ds.DataSourceAlias, out var queryModelBinding))
                        throw new InvalidOperationException($"QueryModelBinding was not found for Data source '{ds.DataSourceAlias}'.");
                    //var dataSourceBindings = queryModelBinding.GetBindings().Select(x => new SqlExpressionBinding(x.SqlExpression, binding.ModelPath.Append(x.ModelPath))).ToArray();
                    var dataSourceBindings = queryModelBinding.PrependPath(binding.ModelPath);  
                    expandedBindings.AddRange(dataSourceBindings);
                }
                else
                {
                    expandedBindings.Add(binding);
                }
            }
            return expandedBindings.ToArray();
        }

        private IEnumerable<SelectColumn> FixDuplicateColumns(SqlExpressionBinding[] bindings)
        {
            var selectItems = new Dictionary<string, SelectColumn>();
            foreach (var binding in bindings)
            {
                var columnAlias = binding.ModelPath.IsEmpty ? "Col1" : binding.ModelPath.GetLastElementRequired();
                if (selectItems.ContainsKey(columnAlias))
                {
                    columnAlias = this.GenerateUniqueColumnAlias(selectItems, columnAlias);
                }
                var selectItem = new SelectColumn(binding.SqlExpression, columnAlias, binding.ModelPath, scalarColumn: binding.ModelPath.IsEmpty);
                selectItems.Add(columnAlias, selectItem);
            }

            return selectItems.Values;
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

        private class SubQueryProjectionReplacementVisitor : SqlExpressionVisitor
        {
            private class ReferenceReplacementFlag
            {
                public bool IsReplaced { get; set; }
            }

            //private readonly SelectColumn[] subQueryProjections;
            private readonly AliasedDataSource subQueryDataSource;
            private readonly Guid subQueryDataSourceAlias;
            private readonly SqlExpressionHashGenerator hashGenerator;
            private readonly List<(int, SelectColumn)> subQueryProjectionHashMap;
            private readonly Stack<ReferenceReplacementFlag> referenceReplaced = new Stack<ReferenceReplacementFlag>();
            private readonly Stack<SqlExpression> sqlExpressionStack = new Stack<SqlExpression>();
            private readonly Stack<bool> visitingCteDataSource = new Stack<bool>();

            public static SqlExpression FindAndReplace(SelectColumn[] subQueryProjections, AliasedDataSource ds, SqlExpression toFindIn)
            {
                if (toFindIn is null)
                    throw new ArgumentNullException(nameof(toFindIn));
                if (subQueryProjections is null)
                    throw new ArgumentNullException(nameof(subQueryProjections));
                if (ds is null)
                    throw new ArgumentNullException(nameof(ds));
                var visitor = new SubQueryProjectionReplacementVisitor(subQueryProjections, ds);
                var visited = visitor.Visit(toFindIn);
                return visited;
            }

            public SubQueryProjectionReplacementVisitor(SelectColumn[] subQueryProjections, AliasedDataSource ds)
            {
                //this.subQueryProjections = subQueryProjections ?? throw new ArgumentNullException(nameof(subQueryProjections));
                this.subQueryDataSource = ds ?? throw new ArgumentNullException(nameof(ds));
                this.subQueryDataSourceAlias = ds.Alias;
                this.hashGenerator = new SqlExpressionHashGenerator();
                this.subQueryProjectionHashMap = subQueryProjections.Select(x => (this.hashGenerator.Generate(x.ColumnExpression), x)).ToList();
            }

            protected internal override SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
            {
                referenceReplaced.Push(new ReferenceReplacementFlag { IsReplaced = false });
                var visitedNode = base.VisitSqlDerivedTable(node) as SqlDerivedTableExpression
                                    ??
                                    throw new InvalidOperationException($"Expected expression type is '{nameof(SqlDerivedTableExpression)}'.");
                var popped = referenceReplaced.Pop();
                if (popped.IsReplaced && this.visitingCteDataSource.Count > 0 && this.visitingCteDataSource.Peek())
                {
                    // it means in the current derived table an outer data source reference was used
                    // so we need to see if that data source is a CTE reference we need to add it as cross join
                    if (this.subQueryDataSource.QuerySource is SqlCteReferenceExpression sourceCteRef)
                    {
                        if (!visitedNode.Joins.Any(x => x.QuerySource is SqlCteReferenceExpression cteRef && cteRef.CteAlias == sourceCteRef.CteAlias))
                        {
                            var cteReference = new SqlCteReferenceExpression(sourceCteRef.CteAlias);
                            var join = new SqlAliasedJoinSourceExpression(SqlJoinType.Cross, cteReference, this.subQueryDataSource.Alias, joinCondition: null, joinName: null, isNavigationJoin: false, navigationParent: null);
                            var newJoins = visitedNode.Joins.Concat(new[] { join }).ToArray();
                            visitedNode = visitedNode.Update(visitedNode.CteDataSources, visitedNode.FromSource, newJoins, visitedNode.WhereClause, visitedNode.GroupByClause, visitedNode.HavingClause, visitedNode.OrderByClause, visitedNode.SelectColumnCollection);
                        }
                    }
                }
                return visitedNode;
            }

            private ReferenceReplacementFlag CurrentFlag => referenceReplaced.Count > 0 ? referenceReplaced.Peek() : null;

            protected internal override SqlExpression VisitSqlAliasedCteSource(SqlAliasedCteSourceExpression node)
            {
                this.visitingCteDataSource.Push(true);
                var visitedNode = base.VisitSqlAliasedCteSource(node);
                this.visitingCteDataSource.Pop();
                return visitedNode;
            }

            public override SqlExpression Visit(SqlExpression node)
            {
                if (node is null) return null;
                try
                {
                    this.sqlExpressionStack.Push(node);
                        var nodeHash = this.hashGenerator.Generate(node);
                    if (this.subQueryProjectionHashMap.Where(x => x.Item1 == nodeHash).Any())
                    {
                        /*
                            here we are handling the case if inner query is using same expression in 2 columns with different aliases,
                            we want to pick the exact alias, even though it would work but still we want to make query 100% correct
                            e.g.

                         var q = (from e in employees
                                    let result1 = e.Name
                                    let result2 = e.Department
                                    orderby result1, result2
                                    select new { result1, result2, e.Name })
                                    .Select(x=>new { x.result1, x.result2, x.Name });

                        In above example `x.result1` and `x.Name` both are pointing to same column `Name` in inner query
                        which could lead to it to render something like this

                                                                            this should be a_2.Name
                                                                               _____|_____
                                                                              |           |
                        select a_2.result1 as result1, a_2.result2 as result2, a_2.result1 as Name          
                        from (
                                select a_1.Name as result1, a_1.Department as result2, a_1.Name as Name
                                from Employee as a_1
                                order by a_1.Name asc, a_1.Department asc
                            ) as a_2

                        As we can see it is selecting `a_2.result1 as Name` which is *correct* as far as results are concern but
                        does not look right from LINQ to SQL conversion. 
                        This selection is happening because Hash of inner SqlExpression (a_1.Name) is same, so system is 
                        picking first matched.

                        But below we are checking if multiple expressions are matched and parent is SqlCompositeBindingExpression
                        then match the alias as well.

                         */

                        var parentNode = this.sqlExpressionStack.Count > 1 ? this.sqlExpressionStack.ElementAt(1) : null;
                        string parentAlias = null;
                        if (parentNode is SqlCompositeBindingExpression parentCompositeBinding)
                        {
                            var nodeInCompositeBinding = parentCompositeBinding.Bindings.Where(x => x.SqlExpression == node).FirstOrDefault();
                            if (nodeInCompositeBinding != null && !nodeInCompositeBinding.ModelPath.IsEmpty)
                                parentAlias = nodeInCompositeBinding.ModelPath.GetLastElementRequired();
                        }
                        var subQueryProjectionMatched = this.subQueryProjectionHashMap.Where(x => x.Item1 == nodeHash).OrderBy(x => x.Item2.Alias == parentAlias ? 0 : 1).First();
                        if (CurrentFlag != null)
                            CurrentFlag.IsReplaced = true;
                        return new SqlDataSourceColumnExpression(subQueryDataSourceAlias, subQueryProjectionMatched.Item2.Alias);
                    }
                    return base.Visit(node);
                }
                finally
                {
                    this.sqlExpressionStack.Pop();
                }
            }
        }

        private class DataSourceColumnUsageExtractor : SqlExpressionVisitor
        {
            private readonly HashSet<SqlDataSourceColumnExpression> dataSourceColumnUsages = new HashSet<SqlDataSourceColumnExpression>();
            private readonly HashSet<Guid> dataSourcesToSearch;
            private SqlExpression targetExpression;

            public DataSourceColumnUsageExtractor(HashSet<Guid> dataSourcesToSearch)
            {
                this.dataSourcesToSearch = dataSourcesToSearch ?? throw new ArgumentNullException(nameof(dataSourcesToSearch));
            }

            public static DataSourceColumnUsageExtractor FindDataSources(HashSet<Guid> dataSourcesToSearch)
            {
                return new DataSourceColumnUsageExtractor(dataSourcesToSearch);
            }

            public DataSourceColumnUsageExtractor In(SqlExpression sqlExpression)
            {
                if (sqlExpression is null)
                    throw new ArgumentNullException(nameof(sqlExpression));
                this.targetExpression = sqlExpression;
                return this;
            }

            public IReadOnlyCollection<SqlDataSourceColumnExpression> ExtractDataSourceColumnExpressions()
            {
                if (this.targetExpression is null)
                    throw new InvalidOperationException($"SqlExpression is not set, please call In method to set it.");
                this.dataSourceColumnUsages.Clear();
                this.Visit(this.targetExpression);
                return this.ConvertDataSourceColumnUsageToDataSourceColumnExpressions();
            }

            public static IReadOnlyCollection<SqlDataSourceColumnExpression> ExtractDataSourceColumnUsages(SqlExpression sqlExpression, HashSet<Guid> dataSourcesToSearch)
            {
                if (sqlExpression is null)
                    throw new ArgumentNullException(nameof(sqlExpression));
                if (dataSourcesToSearch is null)
                    throw new ArgumentNullException(nameof(dataSourcesToSearch));
                var extractor = new DataSourceColumnUsageExtractor(dataSourcesToSearch);
                extractor.Visit(sqlExpression);
                return extractor.ConvertDataSourceColumnUsageToDataSourceColumnExpressions();
            }

            private IReadOnlyCollection<SqlDataSourceColumnExpression> ConvertDataSourceColumnUsageToDataSourceColumnExpressions()
            {
                return this.dataSourceColumnUsages.ToArray();
            }

            protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
            {
                if (this.dataSourcesToSearch.Contains(node.DataSourceAlias))
                {
                    this.dataSourceColumnUsages.Add(node);
                }
                return base.VisitSqlDataSourceColumn(node);
            }
        }
    }
}
