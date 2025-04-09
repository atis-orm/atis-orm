using Atis.Expressions;
using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Services
{
    public class SqlFunctionBooleanExpressionIdentifier : IBooleanExpressionIdentifier
    {
        public bool IsMatch(ArrayStack expressionStack)
        {
            if (expressionStack.RemainingItems > 0)
            {
                var current = expressionStack.Pop();
                if (current is MethodCallExpression methodCallExpression)
                {
                    return Attribute.IsDefined(methodCallExpression.Method, typeof(SqlFunctionAttribute));
                }
            }
            return false;
        }
    }
}
