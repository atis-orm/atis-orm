using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql
{
    public class SqlExpressionHashGenerator : SqlExpressionVisitor
    {
        private HashCode hashCode;

        /// <inheritdoc />
        public override SqlExpression Visit(SqlExpression node)
        {
            if (node is null)
                this.hashCode.Add(0);
            else
                this.hashCode.Add(node.NodeType);
            return base.Visit(node);
        }
        
        public int GenerateHash(SqlExpression expression)
        {
            this.hashCode = new HashCode();
            Visit(expression);
            return hashCode.ToHashCode();
        }

        protected internal override SqlExpression VisitSqlLiteralExpression(SqlLiteralExpression sqlLiteralExpression)
        {
            if (sqlLiteralExpression.LiteralValue == null)
                this.hashCode.Add(0);
            else
                this.hashCode.Add(sqlLiteralExpression.LiteralValue);
            return base.VisitSqlLiteralExpression(sqlLiteralExpression);
        }

        protected internal override SqlExpression VisitSqlParameterExpression(SqlParameterExpression sqlParameterExpression)
        {
            // TODO: might need to add Type as well along with value in SqlParameterExpression
            if (sqlParameterExpression.Value == null)
                this.hashCode.Add(0);
            else
                this.hashCode.Add(sqlParameterExpression.Value.GetType());
            return base.VisitSqlParameterExpression(sqlParameterExpression);
        }

        protected internal override SqlExpression VisitCteReferenceExpression(SqlCteReferenceExpression sqlCteReferenceExpression)
        {
            this.hashCode.Add(sqlCteReferenceExpression.CteAlias);
            return base.VisitCteReferenceExpression(sqlCteReferenceExpression);
        }

        protected internal override SqlExpression VisitSqlColumnExpression(SqlColumnExpression sqlColumnExpression)
        {
            this.hashCode.Add(sqlColumnExpression.ColumnAlias);
            return base.VisitSqlColumnExpression(sqlColumnExpression);
        }

        protected internal override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
        {
            this.hashCode.Add(sqlDataSourceColumnExpression.ColumnName);
            this.hashCode.Add(sqlDataSourceColumnExpression.DataSource.DataSourceAlias);
            return base.VisitSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
        }

        //protected internal override SqlExpression VisitSqlFromSourceExpression(SqlFromSourceExpression sqlFromSourceExpression)
        //{
        //    this.hashCode.Add(sqlFromSourceExpression.DataSourceAlias);
        //    return base.VisitSqlFromSourceExpression(sqlFromSourceExpression);
        //}

        protected internal override SqlExpression VisitSqlAliasExpression(SqlAliasExpression sqlAliasExpression)
        {
            this.hashCode.Add(sqlAliasExpression.ColumnAlias);
            return base.VisitSqlAliasExpression(sqlAliasExpression);
        }

        protected internal override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
        {
            this.hashCode.Add(sqlDataSourceExpression.DataSourceAlias);
            return base.VisitSqlDataSourceExpression(sqlDataSourceExpression);
        }

        protected internal override SqlExpression VisitSqlFunctionCallExpression(SqlFunctionCallExpression sqlFunctionCallExpression)
        {
            this.hashCode.Add(sqlFunctionCallExpression.FunctionName);
            return base.VisitSqlFunctionCallExpression(sqlFunctionCallExpression);
        }

        protected internal override SqlExpression VisitSqlJoinExpression(SqlJoinExpression sqlJoinExpression)
        {
            this.hashCode.Add(sqlJoinExpression.JoinType);
            return base.VisitSqlJoinExpression(sqlJoinExpression);
        }

        protected internal override SqlExpression VisitSqlOrderByExpression(SqlOrderByExpression sqlOrderByExpression)
        {
            this.hashCode.Add(sqlOrderByExpression.Ascending);
            return base.VisitSqlOrderByExpression(sqlOrderByExpression);
        }

        protected internal override SqlExpression VisitSqlTableExpression(SqlTableExpression sqlTableExpression)
        {
            this.hashCode.Add(sqlTableExpression.TableName);
            this.hashCode.Add(sqlTableExpression.TableColumns);
            return base.VisitSqlTableExpression(sqlTableExpression);
        }
    }
}
