using System;
using System.Linq;

namespace Atis.LinqToSql.SqlExpressions
{
    public class SqlUpdateExpression : SqlExpression
    {
        public SqlUpdateExpression(SqlQueryExpression sqlQuery, SqlDataSourceExpression updatingDataSource, string[] columns, SqlExpression[] values)
        {
            this.SqlQuery = sqlQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
            this.UpdatingDataSource = updatingDataSource ?? throw new ArgumentNullException(nameof(updatingDataSource));
            this.Columns = columns ?? throw new ArgumentNullException(nameof(columns));
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
            if (columns.Length == 0)
                throw new ArgumentException("At least one column must be specified.", nameof(columns));
            if (columns.Length != values.Length)
                throw new ArgumentException("The number of columns must match the number of values.", nameof(columns));
            if (!this.SqlQuery.AllDataSources.Where(x => x == updatingDataSource).Any())
                throw new ArgumentException("The updating data source must be part of the query.", nameof(updatingDataSource));
            if (!(updatingDataSource.QuerySource is SqlTableExpression))
                throw new ArgumentException("The updating data source must be a table.", nameof(updatingDataSource));
        }

        public SqlQueryExpression SqlQuery { get; }
        public SqlDataSourceExpression UpdatingDataSource { get; }
        public string[] Columns { get; }
        public SqlExpression[] Values { get; }

        public override SqlExpressionType NodeType => SqlExpressionType.Update;

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitUpdateSqlExpression(this);
        }

        public SqlExpression Update(SqlQueryExpression sqlQuery, SqlDataSourceExpression updatingDataSource, SqlExpression[] values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != this.Values.Length)
                throw new ArgumentException("The number of values must match the number of columns.", nameof(values));
            if (this.SqlQuery == SqlQuery && this.UpdatingDataSource == updatingDataSource && (this.Values == values || this.Values.SequenceEqual(this.Values)))
            {
                return this;
            }
            return new SqlUpdateExpression(sqlQuery, updatingDataSource, this.Columns, values);
        }

        public override string ToString()
        {
            return $"update {this.UpdatingDataSource}\r\nset {string.Join(",\r\n\t", this.Columns.Zip(this.Values, (c, v) => $"{c} = {v}"))}\r\n{this.SqlQuery}";
        }
    }
}
