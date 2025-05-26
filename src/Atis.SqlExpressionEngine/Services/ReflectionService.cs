using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.SqlExpressionEngine.Services
{
    public class ReflectionService : IReflectionService
    {
        private readonly IExpressionEvaluator expressionEvaluator;

        public ReflectionService(IExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }


        public virtual MemberInfo GetPropertyOrField(Type type, string propertyOrFieldName)
        {
            var propertyInfo = type.GetProperty(propertyOrFieldName);
            if (propertyInfo != null)
                return propertyInfo;
            var fieldInfo = type.GetField(propertyOrFieldName);
            if (fieldInfo != null)
                return fieldInfo;
            return null;
        }

        public virtual object GetPropertyOrFieldValue(object instance, MemberInfo propertyOrField)
        {
            if (propertyOrField is PropertyInfo propInfo)
                return propInfo.GetValue(instance);
            else if (propertyOrField is FieldInfo fieldInfo)
                return fieldInfo.GetValue(instance);
            else
                return null;
        }


        public virtual Type GetEntityTypeFromQueryableType(Type queryableType)
        {
            return queryableType.GetInterfaces()
                                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                .Select(t => t.GetGenericArguments()[0])
                                .FirstOrDefault();
        }

        public virtual object CreateInstance(Type type, object[] ctorArgs)
        {
            return Activator.CreateInstance(type, ctorArgs);
        }

        public virtual PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        public virtual object Evaluate(Expression expression)
        {
            return this.expressionEvaluator.Evaluate(expression);
        }

        public virtual bool CanEvaluate(Expression expression)
        {
            return this.expressionEvaluator.CanEvaluate(expression);
        }

        public virtual Type GetPropertyOrFieldType(MemberInfo member)
        {
            return ((member as PropertyInfo)?.PropertyType
                        ??
                        (member as FieldInfo)?.FieldType)
                        ??
                        throw new InvalidOperationException("Member is not a property or field.");
        }

        public virtual bool IsPrimitiveType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // If the type is nullable, get the underlying type
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type.IsPrimitive  // Covers int, bool, double, etc.
                   || type == typeof(string)    // VARCHAR, TEXT
                   || type == typeof(decimal)   // DECIMAL, NUMERIC
                   || type == typeof(byte[])    // BLOB, VARBINARY
                   || type == typeof(Guid)      // UNIQUEIDENTIFIER
                   || type == typeof(DateTime)  // DATETIME, TIMESTAMP
                   || type == typeof(TimeSpan); // INTERVAL, TIME (some DBs)
        }

        public virtual bool IsQueryableType(Type type)
        {
            if (type == typeof(string))
                return false;

            return type.GetInterfaces()
                       .Append(type) // also check the type itself
                       .Any(x => x.IsGenericType &&
                                 x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public virtual bool IsQueryMethod(Expression node)
        {
            return 
                    (
                        node is MethodCallExpression methodCallExpression &&
                        (
                            (
                                (methodCallExpression.Method.DeclaringType == typeof(Queryable) ||
                                methodCallExpression.Method.DeclaringType == typeof(Enumerable))
                                &&
                                // union is not a chain method it will close the query
                                methodCallExpression.Method.Name != nameof(Queryable.Union)
                            )
                            ||
                            this.IsQueryExtensionMethod(methodCallExpression)
                        )
                    )
                    ||
                    node is ChainedQueryExpression;
        }

        private bool IsQueryExtensionMethod(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.DeclaringType == typeof(QueryExtensions)
                    &&
                    !(methodCallExpression.Method.Name == nameof(QueryExtensions.Schema) || 
                        methodCallExpression.Method.Name == nameof(QueryExtensions.UnionAll))
                    ;
        }

        public virtual bool IsVariable(MemberExpression node)
        {
            return this.CanEvaluate(node);
        }

        /// <summary>
        ///     <para>
        ///         Checks if the specified value is enumerable.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If value is IEnumerable, it returns <c>true</c>. However, if value is a string, it returns <c>false</c>.
        ///     </para>
        /// </remarks>
        /// <param name="value">An object to check.</param>
        /// <returns>Returns <c>true</c> if the value is enumerable but not string; otherwise, <c>false</c>.</returns>
        public bool IsEnumerable(object value)
        {
            return value is System.Collections.IEnumerable && !(value is string);
        }

        public bool IsGroupingType(Type type)
        {
            if (type is null)
                return false;
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        }

        private readonly static string[] aggregateMethodNames = new[] { nameof(Queryable.Count), nameof(Queryable.Max), nameof(Queryable.Min), nameof(Queryable.Sum) };

        public bool IsAggregateMethod(MethodCallExpression methodCallExpression)
        {
            return (methodCallExpression.Method.DeclaringType == typeof(Enumerable)
                    ||
                    methodCallExpression.Method.DeclaringType == typeof(Queryable))
                    &&
                    aggregateMethodNames.Contains(methodCallExpression.Method.Name);
        }
    }
}
