using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Atis.SqlExpressionEngine
{
    public abstract class SqlExpressionVisitor
    {
        public virtual SqlExpression Visit(SqlExpression node) => node?.Accept(this);

        protected T VisitAndConvert<T>(T node) where T : SqlExpression
        {
            if (node == null)
                return default;
            var visited = Visit(node);
            if (visited == null)
                return default;
            if (visited is T converted)
                return converted;
            throw new InvalidOperationException($"Expected {typeof(T).Name}, but got {visited.GetType().Name}");
        }


        protected virtual internal SqlExpression VisitCustom(SqlExpression node)
        {
            return node.VisitChildren(this);
        }

        protected virtual internal SqlExpression VisitSqlTable(SqlTableExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlBinary(SqlBinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);
            return node.Update(left, right, node.NodeType);
        }

        protected virtual internal SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
        {
            var newCteDataSources = new List<SqlAliasedCteSourceExpression>();
            if (node.CteDataSources != null)
            {
                foreach (var cteDataSource in node.CteDataSources)
                {
                    var newCteDataSource = VisitAndConvert(cteDataSource);
                    if (newCteDataSource != cteDataSource)
                    {
                        newCteDataSources.Add(newCteDataSource);
                    }
                    else
                    {
                        newCteDataSources.Add(cteDataSource);
                    }
                }
            }
            var newFromSource = VisitAndConvert(node.FromSource);
            var newJoins = new List<SqlAliasedJoinSourceExpression>();
            if (node.Joins != null)
            {
                foreach (var join in node.Joins)
                {
                    var newJoin = VisitAndConvert(join);
                    if (newJoin != join)
                    {
                        newJoins.Add(newJoin);
                    }
                    else
                    {
                        newJoins.Add(join);
                    }
                }
            }
            var newWhereClause = VisitAndConvert(node.WhereClause);
            var newGroupByClause = new List<SqlExpression>();
            if (node.GroupByClause != null)
            {
                foreach (var groupBy in node.GroupByClause)
                {
                    var newGroupBy = Visit(groupBy);
                    if (newGroupBy != groupBy)
                    {
                        newGroupByClause.Add(newGroupBy);
                    }
                    else
                    {
                        newGroupByClause.Add(groupBy);
                    }
                }
            }
            var newHavingClause = VisitAndConvert(node.HavingClause);
            var newOrderByClause = VisitAndConvert(node.OrderByClause);
            var newSelectList = VisitAndConvert(node.SelectColumnCollection);
            return node.Update(newCteDataSources.ToArray(), newFromSource, newJoins.ToArray(), newWhereClause, newGroupByClause.ToArray(), newHavingClause, newOrderByClause, newSelectList);
        }

        protected virtual internal SqlExpression VisitSqlStandaloneSelect(SqlStandaloneSelectExpression node)
        {
            var queryShape = this.Visit(node.QueryShape);
            return node.Update(queryShape);
        }

        protected virtual internal SqlExpression VisitSqlLiteral(SqlLiteralExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlParameter(SqlParameterExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlAlias(SqlAliasExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlStringFunction(SqlStringFunctionExpression node)
        {
            var stringExpression = this.Visit(node.StringExpression);
            var newArguments = new List<SqlExpression>();
            if (node.Arguments != null)
            {
                foreach (var argument in node.Arguments)
                {
                    newArguments.Add(this.Visit(argument));
                }
            }
            return node.Update(stringExpression, newArguments);
        }

        protected virtual internal SqlExpression VisitSqlFunctionCall(SqlFunctionCallExpression node)
        {
            var arguments = new List<SqlExpression>();
            foreach (var argument in node.Arguments)
            {
                arguments.Add(this.Visit(argument));
            }
            return node.Update(arguments);
        }

        protected virtual internal SqlExpression VisitSqlDefaultIfEmpty(SqlDefaultIfEmptyExpression node)
        {
            var derivedTable = this.VisitAndConvert(node.DerivedTable);
            return node.Update(derivedTable);
        }

        protected virtual internal SqlExpression VisitSqlAliasedFromSource(SqlAliasedFromSourceExpression node)
        {
            var querySource = this.VisitAndConvert(node.QuerySource);
            return node.Update(querySource);
        }

        protected virtual internal SqlExpression VisitSqlAliasedCteSource(SqlAliasedCteSourceExpression node)
        {
            var querySource = this.VisitAndConvert(node.CteBody);
            return node.Update(querySource);
        }

        protected virtual internal SqlExpression VisitSqlAliasedJoinSource(SqlAliasedJoinSourceExpression node)
        {
            var tableSource = this.VisitAndConvert(node.QuerySource);
            var joinCondition = this.Visit(node.JoinCondition);
            return node.Update(tableSource, joinCondition);
        }

        protected virtual internal SqlExpression VisitSqlSelectList(SqlSelectListExpression node)
        {
            var newColumnList = new List<SelectColumn>();
            foreach (var selectColumn in node.SelectColumns)
            {
                var newSelectColumn = Visit(selectColumn.ColumnExpression);
                if (newSelectColumn != selectColumn.ColumnExpression)
                {
                    newColumnList.Add(new SelectColumn(newSelectColumn, selectColumn.Alias, selectColumn.ScalarColumn));
                }
                else
                {
                    newColumnList.Add(selectColumn);
                }
            }
            return node.Update(newColumnList.ToArray());
        }

        protected virtual internal SqlExpression VisitFilterClause(SqlFilterClauseExpression node)
        {
            var newPredicateList = new List<FilterCondition>();
            foreach(var filterCondition in node.FilterConditions)
            {
                var newPredicate = Visit(filterCondition.Predicate);
                if (newPredicate != filterCondition.Predicate)
                {
                    newPredicateList.Add(new FilterCondition(newPredicate, filterCondition.UseOrOperator));
                }
                else
                {
                    newPredicateList.Add(filterCondition);
                }
            }
            return node.Update(newPredicateList.ToArray());
        }

        protected virtual internal SqlExpression VisitSqlOrderByClause(SqlOrderByClauseExpression node)
        {
            var newOrderByColumns = new List<OrderByColumn>();
            foreach (var orderByColumn in node.OrderByColumns)
            {
                var newOrderByColumn = Visit(orderByColumn.ColumnExpression);
                if (newOrderByColumn != orderByColumn.ColumnExpression)
                {
                    newOrderByColumns.Add(new OrderByColumn(newOrderByColumn, orderByColumn.Direction));
                }
                else
                {
                    newOrderByColumns.Add(orderByColumn);
                }
            }
            return node.Update(newOrderByColumns.ToArray());
        }

        protected virtual internal SqlExpression VisitUnionQuery(SqlUnionQueryExpression node)
        {
            var unionItems = new List<UnionItem>();
            foreach (var unionItem in node.Unions)
            {
                var derivedTable = VisitAndConvert(unionItem.DerivedTable);
                if (derivedTable != unionItem.DerivedTable)
                {
                    unionItems.Add(new UnionItem(derivedTable, unionItem.UnionType));
                }
                else
                {
                    unionItems.Add(unionItem);
                }
            }
            return node.Update(unionItems.ToArray());
        }

        protected virtual internal SqlExpression VisitSqlConditional(SqlConditionalExpression node)
        {
            var test = Visit(node.Test);
            var ifTrue = Visit(node.IfTrue);
            var ifFalse = Visit(node.IfFalse);
            return node.Update(test, ifTrue, ifFalse);
        }

        protected virtual internal SqlExpression VisitSqlExists(SqlExistsExpression node)
        {
            var subQuery = VisitAndConvert(node.SubQuery);
            return node.Update(subQuery);
        }

        protected virtual internal SqlExpression VisitSqlLike(SqlLikeExpression node)
        {
            var sqlExpression = Visit(node.Expression);
            var pattern = Visit(node.Pattern);
            return node.Update(sqlExpression, pattern);
        }

        protected virtual internal SqlExpression VisitSqlCollection(SqlCollectionExpression node)
        {
            var items = new List<SqlExpression>();
            foreach (var item in node.SqlExpressions)
            {
                items.Add(this.Visit(item));
            }
            return node.Update(items);
        }

        protected virtual internal SqlExpression VisitSqlDateAdd(SqlDateAddExpression node)
        {
            var interval = Visit(node.Interval);
            var dateExpression = Visit(node.DateExpression);
            return node.Update(interval, dateExpression);
        }

        protected virtual internal SqlExpression VisitSqlDateSubtract(SqlDateSubtractExpression node)
        {
            var startDate = Visit(node.StartDate);
            var endDate = Visit(node.EndDate);
            return node.Update(startDate, endDate);
        }

        protected virtual internal SqlExpression VisitSqlCast(SqlCastExpression node)
        {
            var expression = Visit(node.Expression);
            return node.Update(expression);
        }

        protected virtual internal SqlExpression VisitSqlDatePart(SqlDatePartExpression node)
        {
            var dateExpression = Visit(node.DateExpression);
            return node.Update(dateExpression);
        }

        protected virtual internal SqlExpression VisitInValues(SqlInValuesExpression node)
        {
            var expression = Visit(node.Expression);
            var values = new List<SqlExpression>();
            foreach (var value in node.Values)
            {
                values.Add(Visit(value));
            }
            return node.Update(expression, values.ToArray());
        }

        protected virtual internal SqlExpression VisitNegate(SqlNegateExpression node)
        {
            var operand = Visit(node.Operand);
            return node.Update(operand);
        }

        protected virtual internal SqlExpression VisitSqlNot(SqlNotExpression node)
        {
            var operand = Visit(node.Operand);
            return node.Update(operand);
        }

        protected virtual internal SqlExpression VisitSqlUpdate(SqlUpdateExpression node)
        {
            var newTable = VisitAndConvert(node.Source);
            var newValues = new List<SqlExpression>();
            foreach (var setClause in node.Values)
            {
                var newSetClauseItem = Visit(setClause);
                if (newSetClauseItem != setClause)
                {
                    newValues.Add(newSetClauseItem);
                }
                else
                {
                    newValues.Add(setClause);
                }
            }
            return node.Update(newTable, newValues);
        }

        protected virtual internal SqlExpression VisitSqlDelete(SqlDeleteExpression node)
        {
            var source = VisitAndConvert(node.Source);
            return node.Update(source);
        }

        protected virtual internal SqlExpression VisitSqlQueryable(SqlQueryableExpression node)
        {
            var query = this.VisitAndConvert(node.Query);
            return node.Update(query);
        }

        protected virtual internal SqlExpression VisitSqlComment(SqlCommentExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlFragment(SqlFragmentExpression node)
        {
            return node;
        }

        protected virtual internal SqlExpression VisitSqlCteReference(SqlCteReferenceExpression node)
        {
            return node;
        }

        private IReadOnlyList<SqlMemberAssignment> CreateMemberAssignmentList(IReadOnlyCollection<SqlMemberAssignment> bindings)
        {
            var newList = new List<SqlMemberAssignment>();
            foreach (var binding in bindings)
            {
                var newBinding = Visit(binding.SqlExpression);
                if (newBinding != binding.SqlExpression)
                {
                    newList.Add(new SqlMemberAssignment(binding.MemberName, newBinding));
                }
                else
                {
                    newList.Add(binding);
                }
            }
            return newList;
        }

        protected virtual internal SqlExpression VisitSqlMemberInit(SqlMemberInitExpression node)
        {
            var newList = this.CreateMemberAssignmentList(node.Bindings);
            return node.Update(newList);
        }

        protected virtual internal SqlExpression VisitDataSourceQueryShape(SqlDataSourceQueryShapeExpression node)
        {
            var shapeExpression = this.Visit(node.ShapeExpression);
            return node.Update(shapeExpression);
        }

        protected virtual internal SqlExpression VisitSqlInsertInto(SqlInsertIntoExpression node)
        {
            var selectQuery = this.VisitAndConvert(node.SelectQuery);
            return node.Update(selectQuery);
        }

        protected virtual internal SqlExpression VisitSqlNewGuid(SqlNewGuidExpression node)
        {
            return node;
        }
    }
}