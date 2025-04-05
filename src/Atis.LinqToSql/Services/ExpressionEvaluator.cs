using Atis.LinqToSql.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.LinqToSql.Services
{
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        public bool CanEvaluate(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constant:
                    return true;
                case MemberExpression member:
                    return member.Expression is null || CanEvaluate(member.Expression);
                case InvocationExpression invocation:
                    return CanEvaluate(invocation.Expression);
                case NewExpression newExpression:
                    return newExpression.Arguments.All(CanEvaluate) && newExpression.Constructor != null;
                case NewArrayExpression newArray:
                    return newArray.Expressions.All(CanEvaluate);
                default:
                    return false;
            }
        }

        public object Evaluate(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constant:
                    return constant.Value;  // Handles constants (including inlined `const` fields)

                case MemberExpression member:
                    object instance = member.Expression != null ? Evaluate(member.Expression) : null;

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
                    object func = Evaluate(invocation.Expression);
                    return func is Delegate del ? del.DynamicInvoke() : null;  // Handles Func<> properties
                case NewExpression newExpression:
                    object[] constructorArgs = newExpression.Arguments.Select(Evaluate).ToArray();
                    return newExpression.Constructor?.Invoke(constructorArgs);  // Creates new instance
                case NewArrayExpression newArray:
                    Type elementType = newArray.Type.GetElementType();
                    Array array = Array.CreateInstance(elementType, newArray.Expressions.Count);
                    for (int i = 0; i < newArray.Expressions.Count; i++)
                        array.SetValue(Evaluate(newArray.Expressions[i]), i);
                    return array;
                default:
                    throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
            }
        }
    }
}
