using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.LinqToSql
{

    // CAUTION: when visiting the SqlQueryExpression and try to modify it through other methods is NOT 
    // going to work, e.g. you are in SqlDataSourceColumnExpression and you add a new DataSource in
    // the columns' data source, it will not be reflected in the SqlQueryExpression.
    // The VisitSqlQueryExpression method has already taken all the data sources in an array before
    // visiting them, therefore, if we add a new data source in sub-visit, then the array is not
    // going to be changed and thus the actual query might remain same.
    // Another point is that if we try to modify a query that has already been visited then
    // this will not trigger the expression tree update to the point which detects change in expression tree.

    public class SqlExpressionVisitor
    {
        public virtual SqlExpression Visit(SqlExpression node) => node?.Accept(this);

        public T VisitAndConvert<T>(T node) where T : SqlExpression
        {
            if (node == null)
                return default;

            var result = this.Visit(node);
            if (result == null)
                throw new InvalidOperationException("Visit method must return a non-null value.");
            if (!(result is T))
                throw new InvalidOperationException($"Visit method must return a value of type {typeof(T).Name}.");
            return (T)result;
        }

        protected internal virtual SqlExpression VisitSqlBinaryExpression(SqlBinaryExpression sqlBinaryExpression)
        {
            var left = this.Visit(sqlBinaryExpression.Left);
            var right = this.Visit(sqlBinaryExpression.Right);
            return sqlBinaryExpression.Update(left, right, sqlBinaryExpression.NodeType);
        }

        protected internal virtual SqlExpression VisitSqlAliasExpression(SqlAliasExpression sqlAliasExpression)
        {
            // there is no sub-expression to visit
            return sqlAliasExpression;
        }

        protected internal virtual SqlExpression VisitSqlCollectionExpression(SqlCollectionExpression sqlCollectionExpression)
        {
            var items = new List<SqlExpression>();
            foreach (var item in sqlCollectionExpression.SqlExpressions)
            {
                items.Add(this.Visit(item));
            }
            return sqlCollectionExpression.Update(items);
        }

        protected internal virtual SqlExpression VisitSqlColumnExpression(SqlColumnExpression sqlColumnExpression)
        {
            var columnExpression = this.Visit(sqlColumnExpression.ColumnExpression);
            return sqlColumnExpression.Update(columnExpression);
        }

        protected internal virtual SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
        {
            return sqlDataSourceColumnExpression;
        }

        protected internal virtual SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
        {
            var dataSource = this.VisitAndConvert(sqlDataSourceExpression.QuerySource);
            return sqlDataSourceExpression.Update(dataSource);
        }

        protected internal virtual SqlExpression VisitSqlExistsExpression(SqlExistsExpression sqlExistsExpression)
        {
            var subQuery = this.VisitAndConvert(sqlExistsExpression.SqlQuery);
            return sqlExistsExpression.Update(subQuery);
        }

        protected internal virtual SqlExpression VisitSqlFunctionCallExpression(SqlFunctionCallExpression sqlFunctionCallExpression)
        {
            var arguments = new List<SqlExpression>();
            foreach (var argument in sqlFunctionCallExpression.Arguments)
            {
                arguments.Add(this.Visit(argument));
            }
            return sqlFunctionCallExpression.Update(arguments);
        }

        protected internal virtual SqlExpression VisitSqlJoinExpression(SqlJoinExpression sqlJoinExpression)
        {
            var joinedSource = this.VisitAndConvert(sqlJoinExpression.JoinedSource);
            var joinCondition = this.Visit(sqlJoinExpression.JoinCondition);
            return sqlJoinExpression.Update(joinedSource, joinCondition);
        }

        protected internal virtual SqlExpression VisitSqlLiteralExpression(SqlLiteralExpression sqlLiteralExpression)
        {
            return sqlLiteralExpression;
        }

        protected internal virtual SqlExpression VisitSqlOrderByExpression(SqlOrderByExpression sqlOrderByExpression)
        {
            var expression = this.Visit(sqlOrderByExpression.Expression);
            return sqlOrderByExpression.Update(expression);
        }

        protected internal virtual SqlExpression VisitSqlParameterExpression(SqlParameterExpression sqlParameterExpression)
        {
            return sqlParameterExpression;
        }

        protected internal virtual SqlExpression VisitSqlQueryExpression(SqlQueryExpression sqlQueryExpression)
        {
            var cteDataSources = new List<SqlDataSourceExpression>();
            foreach (var cteDataSource in sqlQueryExpression.CteDataSources)
            {
                cteDataSources.Add(this.VisitAndConvert(cteDataSource));
            }
            if(cteDataSources.FirstOrDefault()?.NodeType != sqlQueryExpression.CteDataSources.FirstOrDefault()?.NodeType)
            {
                throw new InvalidOperationException("The CTE data sources must have the same node type.");
            }
            var initialDataSource = this.VisitAndConvert(sqlQueryExpression.InitialDataSource);
            var joins = new List<SqlJoinExpression>();
            foreach (var join in sqlQueryExpression.Joins)
            {
                joins.Add(this.VisitAndConvert(join));
            }
            var whereClause = new List<FilterPredicate>();
            foreach(var where in sqlQueryExpression.WhereClause)
            {
                var predicate = this.Visit(where.Predicate);
                whereClause.Add(new FilterPredicate { Predicate = predicate, UseOrOperator = where.UseOrOperator });
            }
            var havingClause = new List<FilterPredicate>();
            var groupBy = this.Visit(sqlQueryExpression.GroupBy);
            foreach (var having in sqlQueryExpression.HavingClause)
            {
                var predicate = this.Visit(having.Predicate);
                havingClause.Add(new FilterPredicate { Predicate = predicate, UseOrOperator = having.UseOrOperator });
            }
            var projection = this.Visit(sqlQueryExpression.Projection);
            var orderByClause = new List<SqlOrderByExpression>();
            foreach(var orderBy in sqlQueryExpression.OrderBy)
            {
                orderByClause.Add(this.VisitAndConvert(orderBy));
            }
            var top = this.Visit(sqlQueryExpression.Top);
            var unions = new List<SqlUnionExpression>();
            foreach(var union in sqlQueryExpression.Unions)
            {
                unions.Add(this.VisitAndConvert(union));
            }
            return sqlQueryExpression.Update(initialDataSource, joins, whereClause, groupBy, projection, orderByClause, top, cteDataSources, havingClause, unions);
        }

        protected internal virtual SqlExpression VisitCustom(SqlExpression node)
        {
            return node.VisitChildren(this);
        }

        protected internal virtual SqlExpression VisitSqlTableExpression(SqlTableExpression sqlTableExpression)
        {
            return sqlTableExpression;
        }

        //protected internal virtual SqlExpression VisitSqlFromSourceExpression(SqlFromSourceExpression sqlFromSourceExpression)
        //{
        //    var dataSource = this.VisitAndConvert(sqlFromSourceExpression.DataSource);
        //    return sqlFromSourceExpression.Update(dataSource);
        //}

        protected internal virtual SqlExpression VisitSqlUnionExpression(SqlUnionExpression sqlUnionExpression)
        {
            var updatedQuery = this.VisitAndConvert(sqlUnionExpression.Query);
            return sqlUnionExpression.Update(updatedQuery);
        }

        protected internal virtual SqlExpression VisitCteReferenceExpression(SqlCteReferenceExpression sqlCteReferenceExpression)
        {
            return sqlCteReferenceExpression;
        }

        protected internal virtual SqlExpression VisitSqlConditionalExpression(SqlConditionalExpression sqlConditionalExpression)
        {
            var test = this.Visit(sqlConditionalExpression.Test);
            var ifTrue = this.Visit(sqlConditionalExpression.IfTrue);
            var ifFalse = this.Visit(sqlConditionalExpression.IfFalse);
            return sqlConditionalExpression.Update(test, ifTrue, ifFalse);
        }

        protected internal SqlExpression VisitUpdateSqlExpression(SqlUpdateExpression updateSqlExpression)
        {
            var sqlQuery = this.VisitAndConvert(updateSqlExpression.SqlQuery);
            var updatingDataSource = this.VisitAndConvert(updateSqlExpression.UpdatingDataSource);
            var values = updateSqlExpression.Values.Select(this.Visit).ToArray();
            return updateSqlExpression.Update(sqlQuery, updatingDataSource, values);
        }

        protected internal SqlExpression VisitDeleteSqlExpression(SqlDeleteExpression sqlDeleteExpression)
        {
            var sqlQuery = this.VisitAndConvert(sqlDeleteExpression.SqlQuery);
            var deletingDataSource = this.VisitAndConvert(sqlDeleteExpression.DeletingDataSource);
            return sqlDeleteExpression.Update(sqlQuery, deletingDataSource);
        }

        protected internal SqlExpression VisitSqlNotExpression(SqlNotExpression sqlNotExpression)
        {
            var operand = this.Visit(sqlNotExpression.Operand);
            return sqlNotExpression.Update(operand);
        }

        protected internal SqlExpression VisitDataSourceReferenceExpression(SqlDataSourceReferenceExpression sqlDataSourceReferenceExpression)
        {
            return sqlDataSourceReferenceExpression;
        }

        protected internal SqlExpression VisitSelectedCollectionExpression(SqlSelectedCollectionExpression sqlSelectedCollectionExpression)
        {
            return sqlSelectedCollectionExpression;
        }

        protected internal SqlExpression VisitInValuesExpression(SqlInValuesExpression sqlInValuesExpression)
        {
            var expression = this.Visit(sqlInValuesExpression.Expression);
            var values = sqlInValuesExpression.Values.Select(this.Visit).ToArray();
            return sqlInValuesExpression.Update(expression, values);
        }

        //protected internal SqlExpression VisitSqlSubQueryColumnExpression(SqlSubQueryColumnExpression sqlSubQueryColumnExpression)
        //{
        //    var columnExpression = this.VisitAndConvert(sqlSubQueryColumnExpression.ColumnExpression);
        //    return sqlSubQueryColumnExpression.Update(columnExpression);
        //}
    }
}
