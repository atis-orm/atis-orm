using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlInValuesExpression : SqlExpression
    {
        public SqlInValuesExpression(SqlExpression expression, IReadOnlyList<SqlExpression> values)
        {
            this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public SqlExpression Expression { get; }
        public IReadOnlyList<SqlExpression> Values { get; }
        public override SqlExpressionType NodeType => SqlExpressionType.InValues;

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitInValues(this);
        }

        public SqlExpression Update(SqlExpression expression, SqlExpression[] values)
        {
            if (expression == this.Expression && values.SequenceEqual(this.Values))
            {
                return this;
            }
            return new SqlInValuesExpression(expression, values);
        }
    }
}
