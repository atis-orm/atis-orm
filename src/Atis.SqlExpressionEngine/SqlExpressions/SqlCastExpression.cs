using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlCastExpression : SqlExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.Cast;

        public SqlExpression Expression { get; }
        public ISqlDataType SqlDataType { get; }

        public SqlCastExpression(SqlExpression expression, ISqlDataType sqlDataType)
        {
            this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            this.SqlDataType = sqlDataType ?? throw new ArgumentNullException(nameof(sqlDataType));
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlCast(this);
        }

        public SqlCastExpression Update(SqlExpression sqlExpression)
        {
            if (sqlExpression == this.Expression)
            {
                return this;
            }
            return new SqlCastExpression(sqlExpression, this.SqlDataType);
        }

        public override string ToString()
        {
            return $"cast({this.Expression} as {this.SqlDataType.DbType})";
        }
    }
}
