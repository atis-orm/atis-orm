using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Postprocessor
{
    public class ExistsBooleanComparisonPostprocessor : SqlExpressionVisitor, ISqlExpressionPostprocessor
    {
        public void Initialize()
        {
            // do nothing
        }

        public SqlExpression Postprocess(SqlExpression sqlExpression)
        {
            return this.Visit(sqlExpression);
        }

        protected override SqlExpression VisitSqlBinaryExpression(SqlBinaryExpression sqlBinaryExpression)
        {
            if (sqlBinaryExpression.Left.NodeType == SqlExpressionType.Exists &&
                sqlBinaryExpression.Right is SqlLiteralExpression sqlLiteral &&
                sqlLiteral.LiteralValue is bool booleanValue)
            {
                if (booleanValue)
                {
                    return sqlBinaryExpression.Left;
                }
                else
                {
                    return new SqlNotExpression(this.Visit(sqlBinaryExpression.Left));
                }
            }
            return base.VisitSqlBinaryExpression(sqlBinaryExpression);
        }
    }
}
