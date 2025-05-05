using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlNegateExpression : SqlExpression
    {
        public SqlNegateExpression(SqlExpression operand)
        {
            Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        }

        public override SqlExpressionType NodeType => SqlExpressionType.Negate;
        public SqlExpression Operand { get; }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitNegate(this);
        }

        public SqlNegateExpression Update(SqlExpression operand)
        {
            if (operand == Operand)
            {
                return this;
            }
            return new SqlNegateExpression(operand);
        }

        public override string ToString()
        {
            return $"-{Operand}";
        }
    }
}
