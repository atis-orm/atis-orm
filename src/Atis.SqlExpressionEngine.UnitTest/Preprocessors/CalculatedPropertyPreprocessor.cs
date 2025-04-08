using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.SqlExpressionEngine.UnitTest.Preprocessors
{
    public class CalculatedPropertyPreprocessor : CalculatedPropertyPreprocessorBase
    {
        private readonly IReflectionService reflectionService;

        public CalculatedPropertyPreprocessor(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }

        private MemberInfo ResolveMember(MemberExpression memberExpression)
        {
            var resolvedMember = memberExpression.Member;
            if (memberExpression.Expression?.Type != null && resolvedMember.ReflectedType != memberExpression.Expression.Type)
            {
                resolvedMember = this.reflectionService.GetPropertyOrField(memberExpression.Expression.Type, resolvedMember.Name);
            }
            return resolvedMember;
        }

        protected override bool TryGetCalculatedExpression(MemberExpression memberExpression, out LambdaExpression? calculatedPropertyExpression)
        {
            var memberInfo = this.ResolveMember(memberExpression);
            if (memberInfo != null)
            {
                var calculatedPropertyAttribute = memberInfo.GetCustomAttribute<CalculatedPropertyAttribute>();
                if (calculatedPropertyAttribute != null)
                {
                    if (!this.reflectionService.IsPrimitiveType(this.reflectionService.GetPropertyOrFieldType(memberInfo)))
                        throw new InvalidOperationException($"Calculated property '{memberInfo.Name}' must be a primitive type. Use relation navigation to create outer apply relation.");
                    var exprProp = memberInfo?.ReflectedType?.GetField(calculatedPropertyAttribute.ExpressionPropertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (exprProp != null && exprProp.GetValue(null) is LambdaExpression calcExpr)
                    {
                        calculatedPropertyExpression = calcExpr;
                        return true;
                    }
                }
            }
            calculatedPropertyExpression = null;
            return false;
        }
    }
}
