using Atis.LinqToSql.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.LinqToSql
{
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        public object Eval(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constant:
                    return constant.Value;  // Handles constants (including inlined `const` fields)

                case MemberExpression member:
                    object instance = member.Expression != null ? Eval(member.Expression) : null;

                    if (member.Member is PropertyInfo property)
                    {
                        return property.GetMethod.IsStatic
                            ? property.GetValue(null)  // Handles static properties like DateTime.Now
                            : property.GetValue(instance);
                    }
                    if (member.Member is FieldInfo field)
                    {
                        return field.IsStatic
                            ? field.GetValue(null)  // Handles static readonly fields
                            : field.GetValue(instance);
                    }

                    throw new NotSupportedException($"Unsupported member type: {member.Member.GetType()}");

                case InvocationExpression invocation:
                    object func = Eval(invocation.Expression);
                    return func is Delegate del ? del.DynamicInvoke() : null;  // Handles Func<> properties
                case NewExpression newExpression:
                    object[] constructorArgs = newExpression.Arguments.Select(Eval).ToArray();
                    return newExpression.Constructor?.Invoke(constructorArgs);  // Creates new instance
                default:
                    throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
            }
        }
    }
}
