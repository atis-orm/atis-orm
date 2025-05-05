using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.Services
{
    public class SqlExpressionFactory : ISqlExpressionFactory
    {
        public SqlDerivedTableExpression ConvertSelectQueryToDeriveTable(SqlSelectExpression selectQuery)
            => this.ConvertSelectQueryToDeriveTableInternal(selectQuery, SqlExpressionType.DerivedTable);

        public SqlDerivedTableExpression ConvertSelectQueryToUnwrappableDeriveTable(SqlSelectExpression selectQuery)
            // DO NOT REMOVE UnwrappableDerivedTable enum type, you may NOT find the references used but it's important
            // because we are testing the NodeType == DerivedTable so it's being used in the reverse order
            => this.ConvertSelectQueryToDeriveTableInternal(selectQuery, SqlExpressionType.UnwrappableDerivedTable);

        private SqlDerivedTableExpression ConvertSelectQueryToDeriveTableInternal(SqlSelectExpression selectQuery, SqlExpressionType nodeType)
        {
            if (selectQuery is null)
                throw new ArgumentNullException(nameof(selectQuery));

            selectQuery.ApplyAutoProjection();
            var cteDataSources = selectQuery.CteDataSources
                                                .Select(x => new SqlAliasedCteSourceExpression(x.CteBody, x.CteAlias))
                                                .ToArray();
            if (nodeType == SqlExpressionType.UnwrappableDerivedTable &&
                cteDataSources.Length > 0)
                throw new InvalidOperationException($"nodeType is '{nodeType}' while there are CTE Data Sources being extracted from selectQuery, CTE Data Sources are not allowed in this case. This is because for the UnwrappableDerivedTable, the SqlDerivedTableExpression must not be changed when creating a SqlSelectExpression from it, however, it will change the expression if there are CTE Data Sources present, this is a safety measure to prevent more complex errors.");
            var firstDataSource = selectQuery.DataSources.Where(x => !(x is JoinDataSource)).First();
            var fromDataSource = new SqlAliasedFromSourceExpression(firstDataSource.QuerySource, firstDataSource.Alias);
            var joinedDataSources = selectQuery.DataSources
                                                .Where(x => x is JoinDataSource)
                                                .Cast<JoinDataSource>()
                                                .Select(x => new SqlAliasedJoinSourceExpression(x.JoinType, x.QuerySource, x.Alias, x.JoinCondition, x.JoinName, x.IsNavigationJoin, x.NavigationParent))
                                                .ToArray();
            SqlFilterClauseExpression whereClause = null;
            if (selectQuery.WhereClause.Count > 0)
                whereClause = new SqlFilterClauseExpression(selectQuery.WhereClause.ToArray(), SqlExpressionType.WhereClause);
            SqlExpression[] groupByClause = null;
            if (selectQuery.GroupByClause != null)
            {
                var groupByExpressions = new List<SqlExpression>();
                if (selectQuery.GroupByClause is SqlCompositeBindingExpression be)
                {
                    groupByExpressions.AddRange(be.Bindings.Select(x => x.SqlExpression));
                }
                else
                {
                    groupByExpressions.Add(selectQuery.GroupByClause);
                }
                groupByClause = groupByExpressions.ToArray();
            }
            SqlFilterClauseExpression havingClause = null;
            if (selectQuery.HavingClause.Count > 0)
                havingClause = new SqlFilterClauseExpression(selectQuery.HavingClause.ToArray(), SqlExpressionType.HavingClause);
            SqlOrderByClauseExpression orderByClause = null;
            if (selectQuery.OrderByClause.Count > 0)
                orderByClause = new SqlOrderByClauseExpression(selectQuery.OrderByClause.ToArray());
            var selectList = new SqlSelectListExpression(selectQuery.SelectList.ToArray());
            var isDistinct = selectQuery.IsDistinct;
            var top = selectQuery.Top;
            var rowOffset = selectQuery.RowOffset;
            var rowsPerPage = selectQuery.RowsPerPage;
            var autoProjection = selectQuery.AutoProjection;
            var tag = selectQuery.Tag;

            return new SqlDerivedTableExpression(
                cteDataSources,
                fromDataSource,
                joinedDataSources,
                whereClause,
                groupByClause,
                havingClause,
                orderByClause,
                selectList,
                isDistinct,
                top,
                rowOffset,
                rowsPerPage,
                autoProjection,
                tag,
                nodeType);
        }

        public SqlAliasExpression CreateAlias(string alias)
        {
            return new SqlAliasExpression(alias);
        }

        public SqlBinaryExpression CreateBinary(SqlExpression left, SqlExpression right, SqlExpressionType sqlExpressionType)
        {
            return new SqlBinaryExpression(left, right, sqlExpressionType);
        }

        public SqlLiteralExpression CreateLiteral(object value)
        {
            return new SqlLiteralExpression(value);
        }

        public SqlSelectExpression CreateSelectQueryByFrom(SqlCompositeBindingExpression dataSources)
        {
            if (!(dataSources?.Bindings.Length > 0))
                throw new ArgumentNullException(nameof(dataSources), "Data sources cannot be null or empty.");
            return new SqlSelectExpression(cteDataSources: null, compositeBinding: dataSources, sqlFactory: this);
        }

        public SqlSelectExpression CreateSelectQueryByTable(SqlTableExpression table)
        {
            return new SqlSelectExpression(cteDataSources: null, table: table, sqlFactory: this);
        }

        public SqlStringFunctionExpression CreateStringFunction(SqlStringFunction stringFunction, SqlExpression stringExpression, SqlExpression[] arguments)
        {
            return new SqlStringFunctionExpression(stringFunction, stringExpression, arguments);
        }

        public SqlTableExpression CreateTable(string tableName, TableColumn[] tableColumns)
        {
            return new SqlTableExpression(tableName, tableColumns);
        }

        public virtual SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments)
        {
            return new SqlFunctionCallExpression(functionName, arguments);
        }

        public SqlSelectExpression CreateSelectQueryFromStandaloneSelect(SelectColumn[] selectColumns)
        {
            var standaloneSelect = new SqlStandaloneSelectExpression(selectColumns);
            var compositeBinding = this.CreateCompositeBindingForSingleExpression(standaloneSelect, ModelPath.Empty);
            var selectSqlQuery = new SqlSelectExpression(cteDataSources: null, compositeBinding: compositeBinding, sqlFactory: this);
            return selectSqlQuery;
        }

        public SqlConditionalExpression CreateCondition(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse)
        {
            return new SqlConditionalExpression(test, ifTrue, ifFalse);
        }

        protected virtual SqlSelectExpression CreateUnwrappedSelectQueryFromDerivedTable(SqlDerivedTableExpression derivedTable)
        {
            if (derivedTable is null)
                throw new ArgumentNullException(nameof(derivedTable));

            if (derivedTable.NodeType == SqlExpressionType.DerivedTable && IsDerivedTableUnwrappable(derivedTable))
            {
                var cteDataSources = derivedTable.CteDataSources
                                                .Select(x => new CteDataSource(x.CteBody, x.CteAlias))
                                                .ToArray();

                var selectQuery = new SqlSelectExpression(cteDataSources, derivedTable.FromSource.QuerySource, this);
                var oldFromAlias = derivedTable.FromSource.Alias;
                var newFromAlias = selectQuery.DataSources.First().Alias;
                var aliasMap = new List<(Guid oldAlias, Guid newAlias)>
                {
                    (oldFromAlias, newFromAlias)
                };
                foreach (var join in derivedTable.Joins.Cast<SqlAliasedJoinSourceExpression>())
                {
                    SqlDataSourceReferenceExpression navigationParent;
                    SqlExpression joinedSource = join.QuerySource;
                    SqlJoinType joinType = join.JoinType;
                    ModelPath navigationPath = new ModelPath(join.JoinName);
                    string navigationName = join.JoinName;
                    Guid? navigationParentAlias;
                    if (join.NavigationParent == null)
                    {
                        navigationParent = selectQuery;
                        navigationParentAlias = null;
                    }
                    else
                    {
                        navigationParentAlias = aliasMap.First(x => x.oldAlias == join.NavigationParent.Value).newAlias;
                        navigationParent = new SqlDataSourceExpression(selectQuery, navigationParentAlias.Value);
                    }
                    var joinDataSource = selectQuery.AddNavigationJoin(navigationParent, joinedSource, joinType, navigationPath, navigationName);
                    var newJoinAlias = joinDataSource.DataSourceAlias;
                    
                    var updatedJoinCondition = ReplaceDataSourceAliasVisitor.Find(join.Alias).In(join.JoinCondition).ReplaceWith(newJoinAlias);
                    updatedJoinCondition = ReplaceDataSourceAliasVisitor.Find(oldFromAlias).In(updatedJoinCondition).ReplaceWith(newFromAlias);

                    selectQuery.UpdateJoin(newJoinAlias, joinType, updatedJoinCondition, navigationName, navigationJoin: true, navigationParent: navigationParentAlias);

                    aliasMap.Add((join.Alias, newJoinAlias));
                }
                if (derivedTable.WhereClause != null)
                {
                    foreach (var filterCondition in derivedTable.WhereClause.FilterConditions)
                    {
                        SqlExpression updatedFilterCondition = filterCondition.Predicate;
                        foreach (var (oldAlias, newAlias) in aliasMap)
                        {
                            updatedFilterCondition = ReplaceDataSourceAliasVisitor.Find(oldAlias).In(updatedFilterCondition).ReplaceWith(newAlias);
                        }
                        selectQuery.ApplyWhere(updatedFilterCondition, filterCondition.UseOrOperator);
                    }
                }
                var dataSourceInSelectList = derivedTable.SelectColumnCollection
                                                        .SelectColumns.GroupBy(x => (x.ColumnExpression as SqlDataSourceColumnExpression)?.DataSourceAlias)                                                        
                                                        .Select(x => x.Key)
                                                        .ToArray();
                if (dataSourceInSelectList.Length == 1 && derivedTable.Joins.Any(y => y.Alias == dataSourceInSelectList[0]))
                {
                    // NOTE: we are simply matching if *any* of the joined data source matched
                    // while we should be picking that join data source and switch to that one 
                    // instead of just switching to the last data source, but this is because
                    // we are catering the SelectMany(x => x.NavChildren) where we switch
                    // to the last added data source, otherwise, for normal navigational joins
                    // we never switch to last data source
                    selectQuery.SwitchBindingToLastDataSource();
                }
                return selectQuery;
            }
            return new SqlSelectExpression(cteDataSources: null, querySource: derivedTable, sqlFactory: this);
        }

        public SqlSelectExpression CreateSelectQueryFromQuerySource(SqlQuerySourceExpression querySource)
        {
            if (querySource is null)
                throw new ArgumentNullException(nameof(querySource));

            if (querySource.NodeType == SqlExpressionType.DerivedTable && querySource is SqlDerivedTableExpression derivedTable)
            {
                return this.CreateUnwrappedSelectQueryFromDerivedTable(derivedTable);
            }
            else
            {
                return new SqlSelectExpression(cteDataSources: null, querySource: querySource, sqlFactory: this);
            }
        }

        private bool IsDerivedTableUnwrappable(SqlDerivedTableExpression derivedTable)
        {
            if (derivedTable is null)
                throw new ArgumentNullException(nameof(derivedTable));
            return derivedTable.AutoProjection &&
                    //derivedTable.CteDataSources.Length == 0 &&
                    (derivedTable.Joins.Length == 0 ||     // either there are no joins
                                                           // or all the joins are navigation joins
                    derivedTable.Joins.All(x => x.IsNavigationJoin)) &&
                    !(derivedTable.HavingClause?.FilterConditions.Length > 0) &&
                    derivedTable.GroupByClause.Length == 0 &&
                    !(derivedTable.OrderByClause?.OrderByColumns.Length > 0) &&
                    derivedTable.Top == null &&
                    derivedTable.IsDistinct == false &&
                    derivedTable.RowOffset == null &&
                    derivedTable.RowsPerPage == null;
        }

        public SqlCompositeBindingExpression CreateCompositeBindingForSingleExpression(SqlExpression sqlExpression, ModelPath modelPath)
        {
            var expressionBinding = new SqlExpressionBinding(sqlExpression, modelPath);
            return this.CreateCompositeBindingForMultipleExpressions(new[] { expressionBinding });
        }

        public SqlCompositeBindingExpression CreateCompositeBindingForMultipleExpressions(SqlExpressionBinding[] sqlExpressionBindings)
        {
            return new SqlCompositeBindingExpression(sqlExpressionBindings);
        }

        public SqlDefaultIfEmptyExpression CreateDefaultIfEmpty(SqlDerivedTableExpression derivedTable)
        {
            return new SqlDefaultIfEmptyExpression(derivedTable);
        }

        public SqlUnionQueryExpression CreateUnionQuery(UnionItem[] unionItems)
        {
            return new SqlUnionQueryExpression(unionItems);
        }

        public SqlExistsExpression CreateExists(SqlDerivedTableExpression subQuery)
        {
            return new SqlExistsExpression(subQuery);
        }

        public SqlLikeExpression CreateLike(SqlExpression stringExpression, SqlExpression pattern)
        {
            return new SqlLikeExpression(stringExpression, pattern, SqlExpressionType.Like);
        }

        public SqlLikeExpression CreateLikeStartsWith(SqlExpression stringExpression, SqlExpression pattern)
        {
            return new SqlLikeExpression(stringExpression, pattern, SqlExpressionType.LikeStartsWith);
        }

        public SqlLikeExpression CreateLikeEndsWith(SqlExpression stringExpression, SqlExpression pattern)
        {
            return new SqlLikeExpression(stringExpression, pattern, SqlExpressionType.LikeEndsWith);
        }

        public SqlDateAddExpression CreateDateAdd(SqlDatePart datePart, SqlExpression interval, SqlExpression dateExpression)
        {
            return new SqlDateAddExpression(datePart, interval, dateExpression);
        }

        public SqlDateSubtractExpression CreateDateSubtract(SqlDatePart datePart, SqlExpression startDate, SqlExpression endDate)
        {
            return new SqlDateSubtractExpression(datePart, startDate, endDate);
        }

        public SqlCollectionExpression CreateCollection(IEnumerable<SqlExpression> sqlExpressions)
        {
            return new SqlCollectionExpression(sqlExpressions);
        }

        public SqlCastExpression CreateCast(SqlExpression expression, ISqlDataType sqlDataType)
        {
            return new SqlCastExpression(expression, sqlDataType);
        }

        public SqlDatePartExpression CreateDatePart(SqlDatePart datePart, SqlExpression dateExpr)
        {
            return new SqlDatePartExpression(datePart, dateExpr);
        }

        public SqlParameterExpression CreateParameter(object value, bool multipleValues)
        {
            return new SqlParameterExpression(value, multipleValues);
        }

        public SqlInValuesExpression CreateInValuesExpression(SqlExpression expression, SqlExpression[] values)
        {
            return new SqlInValuesExpression(expression, values);
        }

        public SqlNegateExpression CreateNegate(SqlExpression operand)
        {
            return new SqlNegateExpression(operand);
        }

        public SqlNotExpression CreateNot(SqlExpression sqlExpression)
        {
            return new SqlNotExpression(sqlExpression);
        }

        public SqlUpdateExpression CreateUpdate(SqlDerivedTableExpression source, Guid dataSourceToUpdate, string[] columns, SqlExpression[] values)
        {
            return new SqlUpdateExpression(source, dataSourceToUpdate, columns, values);
        }

        public SqlDerivedTableExpression ConvertSelectQueryToDataManipulationDerivedTable(SqlSelectExpression selectQuery)
        {
            var tempDerivedTable = this.ConvertSelectQueryToDeriveTable(selectQuery);
            var dmDerivedTable = new SqlDerivedTableExpression(tempDerivedTable.CteDataSources, tempDerivedTable.FromSource, tempDerivedTable.Joins, tempDerivedTable.WhereClause, tempDerivedTable.GroupByClause, tempDerivedTable.HavingClause, tempDerivedTable.OrderByClause, selectColumnCollection: null, tempDerivedTable.IsDistinct, tempDerivedTable.Top, tempDerivedTable.RowOffset, tempDerivedTable.RowsPerPage, tempDerivedTable.AutoProjection, selectQuery.Tag, SqlExpressionType.DataManipulationDerivedTasble);
            return dmDerivedTable;
        }

        public SqlDeleteExpression CreateDelete(SqlDerivedTableExpression source, Guid dataSourceAlias)
        {
            return new SqlDeleteExpression(source, dataSourceAlias);
        }
    }
}
