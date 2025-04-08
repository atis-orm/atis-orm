using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Services
{
    public class SqlFunctionBooleanExpressionIdentifier : IBooleanExpressionIdentifier
    {
        public bool IsBooleanExpression(Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression)
            {
                return Attribute.IsDefined(methodCallExpression.Method, typeof(SqlFunctionAttribute));
            }
            return false;
        }
    }
}
