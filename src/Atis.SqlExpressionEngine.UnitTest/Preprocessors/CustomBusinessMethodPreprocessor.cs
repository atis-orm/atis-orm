using Atis.Expressions;
using Atis.SqlExpressionEngine.UnitTest.Tests;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.SqlExpressionEngine.UnitTest.Preprocessors
{
    public class CustomBusinessMethodPreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        public void Initialize()
        {
            // do nothing
        }

        public Expression Preprocess(Expression expression)
        {
            return this.Visit(expression);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            if (node.Expression is MemberExpression memberExpression)
            {
                var pseudoMethodAttribute = memberExpression.Member.GetCustomAttribute<PseudoMethodAttribute>();
                if (pseudoMethodAttribute != null)
                {
                    var expressionProperty = pseudoMethodAttribute.ExpressionProperty;
                    var propertyInfo = memberExpression.Member.DeclaringType?.GetField(expressionProperty, BindingFlags.Static | BindingFlags.Public);
                    if (propertyInfo != null)
                    {
                        if (propertyInfo.GetValue(null) is LambdaExpression lambdaExpression)
                        {
                            if (lambdaExpression.Parameters.Count != node.Arguments.Count)
                                throw new InvalidOperationException($"The number of arguments does not match the number of parameters in the lambda expression for {expressionProperty}.");
                            var updatedBody = ExpressionReplacementVisitor.Replace(lambdaExpression.Parameters, node.Arguments, lambdaExpression.Body);
                            return updatedBody;
                        }
                    }
                }
            }

            return base.VisitInvocation(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var pseudoMethodAttribute = node.Method.GetCustomAttribute<PseudoMethodAttribute>();
            if (pseudoMethodAttribute != null)
            {
                var expressionProperty = pseudoMethodAttribute.ExpressionProperty;
                var propertyInfo = node.Method.DeclaringType?.GetField(expressionProperty, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo != null)
                {
                    if (propertyInfo.GetValue(null) is LambdaExpression lambdaExpression)
                    {
                        if (lambdaExpression.Parameters.Count != node.Arguments.Count)
                            throw new InvalidOperationException($"The number of arguments does not match the number of parameters in the lambda expression for {expressionProperty}.");
                        var updatedBody = ExpressionReplacementVisitor.Replace(lambdaExpression.Parameters, node.Arguments, lambdaExpression.Body);
                        return updatedBody;
                    }
                }
            }
            return base.VisitMethodCall(node);
        }
    }
}
