using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlQueryableExpression : SqlExpression
    {
        public SqlQueryableExpression(SqlDerivedTableExpression query)
        {
            this.Query = query ?? throw new ArgumentNullException(nameof(query));
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Queryable;
        public SqlDerivedTableExpression Query { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlQueryable(this);
        }

        public SqlQueryableExpression Update(SqlDerivedTableExpression query)
        {
            if (query == Query)
            {
                return this;
            }
            return new SqlQueryableExpression(query);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Queryable: {Query}";
        }
    }
}
