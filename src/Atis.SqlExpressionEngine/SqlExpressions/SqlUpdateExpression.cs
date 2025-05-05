using System;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlUpdateExpression : SqlExpression
    {
        public SqlUpdateExpression(SqlDerivedTableExpression source, Guid updatingDataSource, string[] columns, SqlExpression[] values)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.DataSource = updatingDataSource;
            this.Columns = columns ?? throw new ArgumentNullException(nameof(columns));
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
            if (columns.Length == 0)
                throw new ArgumentException("At least one column must be specified.", nameof(columns));
            if (columns.Length != values.Length)
                throw new ArgumentException("The number of columns must match the number of values.", nameof(columns));
            if (!this.Source.AllDataSources.Where(x => x.Alias == updatingDataSource).Any())
                throw new ArgumentException("The updating data source must be part of the query.", nameof(updatingDataSource));
        }

        public SqlDerivedTableExpression Source { get; }
        public Guid DataSource { get; }
        public string[] Columns { get; }
        public SqlExpression[] Values { get; }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Update;

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlUpdate(this);
        }

        public SqlExpression Update(SqlDerivedTableExpression sqlQuery, SqlExpression[] values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            if (this.Source == Source && (this.Values == values || this.Values.SequenceEqual(this.Values)))
            {
                return this;
            }
            return new SqlUpdateExpression(sqlQuery, this.DataSource, this.Columns, values);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"update {this.DataSource}\r\nset {string.Join(",\r\n\t", this.Columns.Zip(this.Values, (c, v) => $"{c} = {v}"))}\r\n{this.Source}";
        }
    }
}
