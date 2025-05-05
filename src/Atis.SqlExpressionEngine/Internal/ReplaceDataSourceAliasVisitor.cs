using Atis.SqlExpressionEngine.SqlExpressions;
using System;

namespace Atis.SqlExpressionEngine.Internal
{
    class ReplaceDataSourceAliasVisitor : SqlExpressionVisitor
    {
        private Guid dataSourceAliasToReplace;
        private Guid? newDataSourceAlisa;
        private SqlExpression sqlExpression;

        public ReplaceDataSourceAliasVisitor(Guid dataSourceAliasToReplace, Guid? newDataSourceAlisa)
        {
            this.dataSourceAliasToReplace = dataSourceAliasToReplace;
            this.newDataSourceAlisa = newDataSourceAlisa;
        }

        public static ReplaceDataSourceAliasVisitor Find(Guid oldDataSourceAlias)
        {
            return new ReplaceDataSourceAliasVisitor(oldDataSourceAlias, null);
        }

        public ReplaceDataSourceAliasVisitor In(SqlExpression sqlExpressionToSearch)
        {
            this.sqlExpression = sqlExpressionToSearch;
            return this;
        }

        public SqlExpression ReplaceWith(Guid newDataSourceAlias)
        {
            if (this.sqlExpression is null)
                throw new InvalidOperationException("The sqlExpression to search is not set.");
            this.newDataSourceAlisa = newDataSourceAlias;
            return this.Visit(this.sqlExpression);
        }

        public static SqlExpression FindAndReplace(Guid oldDataSourceAlias, Guid newDataSourceAlias, SqlExpression sqlExpressionToSearch)
        {
            var visitor = new ReplaceDataSourceAliasVisitor(oldDataSourceAlias, newDataSourceAlias);
            return visitor.Visit(sqlExpressionToSearch);
        }

        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            if (node.DataSourceAlias == this.dataSourceAliasToReplace)
            {
                return new SqlDataSourceColumnExpression(this.newDataSourceAlisa.Value, node.ColumnName);
            }
            return base.VisitSqlDataSourceColumn(node);
        }
    }
}
