using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlNotExpression : SqlExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.Not;
        public SqlExpression Operand { get; }

        public SqlNotExpression(SqlExpression operand)
        {
            Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlNotExpression(this);
        }

        public override string ToString()
        {
            return $"NOT {Operand}";
        }

        public SqlExpression Update(SqlExpression operand)
        {
            if (this.Operand == operand)
                return this;
            return new SqlNotExpression(operand);
        }
    }
}
