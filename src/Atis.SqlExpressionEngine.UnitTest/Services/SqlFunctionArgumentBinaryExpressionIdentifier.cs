using Atis.Expressions;
using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Services
{
    public class SqlFunctionArgumentBinaryExpressionIdentifier : IBooleanExpressionIdentifier
    {
        public bool IsMatch(ArrayStack expressionStack)
        {
            var current = expressionStack.Pop();
            var parent = expressionStack.Pop();
            if (parent is MethodCallExpression methodCallExpression &&
                Attribute.IsDefined(methodCallExpression.Method, typeof(SqlFunctionAttribute)) &&
                 methodCallExpression.Arguments.Contains(current) &&
                 (current.NodeType == ExpressionType.Not || current is BinaryExpression))
            {
                return true;
            }
            return false;
        }
    }
}
