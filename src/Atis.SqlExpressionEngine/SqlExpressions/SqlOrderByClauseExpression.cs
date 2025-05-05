using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlOrderByClauseExpression : SqlExpression
    {
        public SqlOrderByClauseExpression(OrderByColumn[] orderByColumns)
        {
            if (!(orderByColumns?.Length > 0))
                throw new ArgumentNullException(nameof(orderByColumns), "Order by columns cannot be null or empty.");
            this.OrderByColumns = orderByColumns;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.OrderByClause;
        public OrderByColumn[] OrderByColumns { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlOrderByClause(this);
        }

        public SqlOrderByClauseExpression Update(OrderByColumn[] orderByColumns)
        {
            if (this.OrderByColumns.AllEqual(orderByColumns))
                return this;
            return new SqlOrderByClauseExpression(orderByColumns);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var orderByColumns = string.Join(", ", this.OrderByColumns.Select(x => x.ToString()));
            return $"order by {orderByColumns}";
        }
    }
}
