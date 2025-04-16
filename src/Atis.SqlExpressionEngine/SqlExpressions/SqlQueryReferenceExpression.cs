using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlQueryReferenceExpression : SqlReferenceExpression<SqlQueryExpression>
    {
        public SqlQueryReferenceExpression(SqlQueryExpression reference) : base(reference)
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"query-ref: {this.Reference}";
        }
    }
}
