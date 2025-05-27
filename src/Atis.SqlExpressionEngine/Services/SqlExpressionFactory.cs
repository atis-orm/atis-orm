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

        private void FlattenSqlExpressions(SqlExpression sqlExpression, List<SqlExpression> sqlExpressions)
        {
            if (sqlExpression is SqlMemberInitExpression queryShape)
            {
                foreach (var binding in queryShape.Bindings)
                {
                    this.FlattenSqlExpressions(binding.SqlExpression, sqlExpressions);
                }
            }
            else
                sqlExpressions.Add(sqlExpression);
        }

        private SqlExpression CreateCopyOfQueryShape(SqlExpression queryShape, bool autoProjection)
        {
            if (queryShape is null)
                throw new ArgumentNullException(nameof(queryShape));

            return convert(queryShape);

            SqlExpression convert(SqlExpression sqlExpression)
            {
                if (sqlExpression is SqlMemberInitExpression qs)
                {
                    IEnumerable<SqlMemberAssignment> bindings = qs.Bindings;
                    if (!autoProjection)
                        bindings = bindings.Where(x => x.Projectable);
                    var newList = bindings.Select(x => new SqlMemberAssignment(x.MemberName, convert(x.SqlExpression), x.Projectable)).ToArray();
                    return new SqlMemberInitExpression(newList);
                }
                else if (sqlExpression is SqlDataSourceQueryShapeExpression dsQueryShape)
                {
                    var newShape = convert(dsQueryShape.ShapeExpression);
                    return new SqlDataSourceQueryShapeExpression(newShape, dsQueryShape.DataSourceAlias);
                }
                return sqlExpression;
            }
        }

        private SqlDerivedTableExpression ConvertSelectQueryToDeriveTableInternal(SqlSelectExpression selectQuery, SqlExpressionType nodeType)
        {
            if (selectQuery is null)
                throw new ArgumentNullException(nameof(selectQuery));

            var autoProjection = selectQuery.SelectList.Count == 0;
            // we should NOT be modifying the given selectQuery instance
            // previously we were doing sqlQuery.ApplyAutoProjection()
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
                                                .Select(x => new SqlAliasedJoinSourceExpression(x.JoinType, x.QuerySource, x.Alias, x.JoinCondition, x.JoinName, x.IsNavigationJoin))
                                                .ToArray();
            SqlFilterClauseExpression whereClause = null;
            if (selectQuery.WhereClause.Count > 0)
                whereClause = new SqlFilterClauseExpression(selectQuery.WhereClause.ToArray(), SqlExpressionType.WhereClause);
            SqlExpression[] groupByClause = null;
            SqlExpression groupClause = selectQuery.GroupByClause != null ? this.CreateCopyOfQueryShape(selectQuery.GroupByClause, autoProjection) : null;
            if (groupClause != null)
            {
                var groupExpressions = new List<SqlExpression>();
                this.FlattenSqlExpressions(groupClause, groupExpressions);
                groupByClause = groupExpressions.ToArray();
            }
            SqlFilterClauseExpression havingClause = null;
            if (selectQuery.HavingClause.Count > 0)
                havingClause = new SqlFilterClauseExpression(selectQuery.HavingClause.ToArray(), SqlExpressionType.HavingClause);
            SqlOrderByClauseExpression orderByClause = null;
            if (selectQuery.OrderByClause.Count > 0)
                orderByClause = new SqlOrderByClauseExpression(selectQuery.OrderByClause.ToArray());
            SqlExpression queryShape;
            var selectQueryShape = selectQuery.GetQueryShapeForFieldMapping();
            selectQueryShape = (selectQueryShape as SqlQueryShapeFieldResolverExpression)?.ShapeExpression
                                ??
                                selectQueryShape;
            selectQueryShape = this.CreateCopyOfQueryShape(selectQueryShape, autoProjection);
            queryShape = selectQueryShape;
            IReadOnlyList<SelectColumn> columns;
            if (selectQuery.SelectList.Count > 0)
                columns = selectQuery.SelectList.Select(x => new SelectColumn(x.ColumnExpression, x.Alias, x.ScalarColumn)).ToArray();
            else
            {
                if (groupClause == null)
                    columns = ExtensionMethods.ConvertQueryShapeToSelectList(queryShape, applyAll: false);
                else
                    columns = ExtensionMethods.ConvertQueryShapeToSelectList(groupClause, applyAll: false);
            }
            var selectList = new SqlSelectListExpression(columns);
            var isDistinct = selectQuery.IsDistinct;
            var top = selectQuery.Top;
            var rowOffset = selectQuery.RowOffset;
            var rowsPerPage = selectQuery.RowsPerPage;
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
                    queryShape,
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

        public SqlStringFunctionExpression CreateStringFunction(SqlStringFunction stringFunction, SqlExpression stringExpression, IReadOnlyList<SqlExpression> arguments)
        {
            return new SqlStringFunctionExpression(stringFunction, stringExpression, arguments);
        }

        public SqlTableExpression CreateTable(string tableName, IReadOnlyList<TableColumn> tableColumns)
        {
            return new SqlTableExpression(tableName, tableColumns);
        }

        public virtual SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments)
        {
            return new SqlFunctionCallExpression(functionName, arguments);
        }

        public SqlSelectExpression CreateSelectQueryFromStandaloneSelect(SqlStandaloneSelectExpression standaloneSelect)
        {
            if (standaloneSelect is null)
                throw new ArgumentNullException(nameof(standaloneSelect));

            var selectSqlQuery = new SqlSelectExpression(cteDataSources: null, standaloneSelect, sqlFactory: this);
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
                var fromSource = derivedTable.FromSource;
                var selectQuery = new SqlSelectExpression(cteDataSources, fromSource.QuerySource, this);
                var oldFromAlias = derivedTable.FromSource.Alias;
                var newFromAlias = selectQuery.DataSources.First().Alias;
                var aliasMap = new Dictionary<Guid, Guid>
                {
                    { oldFromAlias, newFromAlias }
                };
                foreach (var join in derivedTable.Joins)
                {
                    var updatedQuerySource = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, join.QuerySource) as SqlQuerySourceExpression
                                            ??
                                            throw new InvalidOperationException($"Failed to convert {join.QuerySource} to SqlQuerySourceExpression");
                    var dsQueryShape = selectQuery.AddJoin(updatedQuerySource, join.JoinType);
                    aliasMap.Add(join.Alias, dsQueryShape.DataSourceAlias);
                    var joinCondition = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, join.JoinCondition);
                    selectQuery.UpdateJoin(dsQueryShape.DataSourceAlias, join.JoinType, joinCondition, join.JoinName, join.IsNavigationJoin);
                }
                if (!(derivedTable.WhereClause?.FilterConditions.IsNullOrEmpty() ?? true))
                {
                    foreach (var filterCondition in derivedTable.WhereClause.FilterConditions)
                    {
                        var updatedFilterCondition = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, filterCondition.Predicate);
                        selectQuery.ApplyWhere(updatedFilterCondition, filterCondition.UseOrOperator);
                    }
                }
                if (!derivedTable.GroupByClause.IsNullOrEmpty())
                {
                    throw new NotImplementedException();
                }
                if (!(derivedTable.HavingClause?.FilterConditions.IsNullOrEmpty() ?? true))
                {
                    foreach (var filterCondition in derivedTable.HavingClause.FilterConditions)
                    {
                        var updatedFilterCondition = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, filterCondition.Predicate);
                        selectQuery.ApplyHaving(updatedFilterCondition, filterCondition.UseOrOperator);
                    }
                }
                var orderByColumns = derivedTable.OrderByClause?.OrderByColumns;
                var orderByColumnsNonAliases = orderByColumns?.Where(x => !(x.ColumnExpression is SqlAliasExpression)).ToArray();
                var orderByColumnsAliases = orderByColumns?.Where(x => x.ColumnExpression is SqlAliasExpression).ToArray();
                if (orderByColumnsNonAliases != null)
                {
                    foreach (var orderBy in orderByColumnsNonAliases)
                    {
                        var updatedOrderBy = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, orderBy.ColumnExpression);
                        selectQuery.ApplyOrderBy(updatedOrderBy, orderBy.Direction);
                    }
                }
                var updatedQueryShape = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, derivedTable.QueryShape);
                if (derivedTable.SelectColumnCollection?.SelectColumns.Count > 0 && !derivedTable.AutoProjection)
                {
                    if (derivedTable.QueryShape == null)
                        throw new InvalidOperationException($"derivedTable.QueryShape is null");
                    selectQuery.ApplyProjection(updatedQueryShape);
                }
                else
                {
                    selectQuery.UpdateModelBinding(updatedQueryShape);
                }
                if (orderByColumnsAliases != null)
                {
                    foreach (var orderBy in orderByColumnsAliases)
                    {
                        // no need to update since they are all aliases
                        selectQuery.ApplyOrderBy(orderBy.ColumnExpression, orderBy.Direction);
                    }
                }
                if (derivedTable.RowOffset != null)
                {
                    selectQuery.ApplyRowOffset(derivedTable.RowOffset.Value);
                }
                if (derivedTable.RowsPerPage != null)
                {
                    selectQuery.ApplyRowsPerPage(derivedTable.RowsPerPage.Value);
                }
                if (derivedTable.Top != null)
                {
                    selectQuery.ApplyTop(derivedTable.Top.Value);
                }
                return selectQuery;
            }
            return new SqlSelectExpression(cteDataSources: null, derivedTable, sqlFactory: this);
        }

        public SqlSelectExpression CreateSelectQuery(SqlExpression queryShape)
        {
            if (queryShape is null)
                throw new ArgumentNullException(nameof(queryShape));

            if (queryShape.NodeType == SqlExpressionType.DerivedTable && queryShape is SqlDerivedTableExpression derivedTable)
            {
                return this.CreateUnwrappedSelectQueryFromDerivedTable(derivedTable);
            }
            else
            {
                return new SqlSelectExpression(cteDataSources: null, queryShape, sqlFactory: this);
            }
        }

        private bool IsDerivedTableUnwrappable(SqlDerivedTableExpression derivedTable)
        {
            if (derivedTable is null)
                throw new ArgumentNullException(nameof(derivedTable));
            return derivedTable.AutoProjection &&
                    //derivedTable.CteDataSources.Length == 0 &&
                    (derivedTable.Joins.Count == 0 ||     // either there are no joins
                                                           // or all the joins are navigation joins
                    derivedTable.Joins.All(x => x.IsNavigationJoin)) &&
                    !(derivedTable.HavingClause?.FilterConditions.Length > 0) &&
                    derivedTable.GroupByClause.Count == 0 &&
                    !(derivedTable.OrderByClause?.OrderByColumns.Count > 0) &&
                    derivedTable.Top == null &&
                    derivedTable.IsDistinct == false &&
                    derivedTable.RowOffset == null &&
                    derivedTable.RowsPerPage == null;
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
            var dmDerivedTable = new SqlDerivedTableExpression(tempDerivedTable.CteDataSources, tempDerivedTable.FromSource, tempDerivedTable.Joins, tempDerivedTable.WhereClause, tempDerivedTable.GroupByClause, tempDerivedTable.HavingClause, tempDerivedTable.OrderByClause, selectColumnCollection: null, tempDerivedTable.IsDistinct, tempDerivedTable.Top, tempDerivedTable.RowOffset, tempDerivedTable.RowsPerPage, tempDerivedTable.AutoProjection, selectQuery.Tag, selectQuery.GetQueryShapeForFieldMapping(), SqlExpressionType.DataManipulationDerivedTable);
            return dmDerivedTable;
        }

        public SqlDeleteExpression CreateDelete(SqlDerivedTableExpression source, Guid dataSourceAlias)
        {
            return new SqlDeleteExpression(source, dataSourceAlias);
        }

        public SqlExpression CreateJoinCondition(SqlExpression leftSide, SqlExpression rightSide)
        {
            SqlBinaryExpression joinPredicate = null;

            var leftExpressions = (leftSide as SqlQueryShapeExpression)?.FlattenQueryShape()
                                    ??
                                    new[] { leftSide };
            var rightExpressions = (rightSide as SqlQueryShapeExpression)?.FlattenQueryShape()
                                    ??
                                    new[] { rightSide };


            if (leftExpressions.Count != rightExpressions.Count)
                throw new InvalidOperationException($"Source columns count {leftExpressions.Count} does not match other columns count {rightExpressions.Count}.");

            for (var i = 0; i < leftExpressions.Count; i++)
            {
                var leftSideExpression = leftExpressions[i];
                var rightSideExpression = rightExpressions[i];
                var condition = this.CreateBinary(leftSideExpression, rightSideExpression, SqlExpressionType.Equal);
                joinPredicate = joinPredicate == null ? condition : this.CreateBinary(joinPredicate, condition, SqlExpressionType.AndAlso);
            }

            return joinPredicate;
        }
    }
}
