using System;
using System.Linq;

namespace Atis.LinqToSql.SqlExpressions
{
    public class SqlDeleteExpression : SqlExpression
    {
        public SqlDeleteExpression(SqlQueryExpression sqlQuery, SqlDataSourceExpression deletingDataSource)
        {
            this.SqlQuery = sqlQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
            this.DeletingDataSource = deletingDataSource ?? throw new ArgumentNullException(nameof(deletingDataSource));
            if (!this.SqlQuery.AllQuerySources.Where(x => x == deletingDataSource).Any())
                throw new ArgumentException("The deleting data source must be part of the query.", nameof(deletingDataSource));
            if (!(deletingDataSource.QuerySource is SqlTableExpression))
                throw new ArgumentException("The deleting data source must be a table.", nameof(deletingDataSource));
        }

        public SqlQueryExpression SqlQuery { get; }
        public SqlDataSourceExpression DeletingDataSource { get; }

        public override SqlExpressionType NodeType => SqlExpressionType.Delete;

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitDeleteSqlExpression(this);
        }

        public SqlExpression Update(SqlQueryExpression sqlQuery, SqlDataSourceExpression updatingDataSource)
        {
            if (this.SqlQuery == SqlQuery && this.DeletingDataSource == updatingDataSource)
            {
                return this;
            }
            return new SqlDeleteExpression(sqlQuery, updatingDataSource);
        }

        public override string ToString()
        {
            return $"delete {this.DeletingDataSource}\r\n{this.SqlQuery}";
        }
    }
}
