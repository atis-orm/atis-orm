using Atis.LinqToSql.ExpressionExtensions;
using Atis.LinqToSql.Infrastructure;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.LinqToSql
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


        public virtual Expression GetQueryExpressionFromQueryable(object queryableObject)
        {
            return (queryableObject as IQueryable)?.Expression;
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

        public virtual object Eval(Expression expression)
        {
            return this.expressionEvaluator.Eval(expression);
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
            return typeof(IQueryable).IsAssignableFrom(type);
        }

        public virtual bool IsQuerySource(Expression node)
        {
            return (
                    node is MethodCallExpression methodCallExpression &&
                            (
                                (methodCallExpression.Method.Name == nameof(QueryExtensions.From) &&
                                methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
                                ||
                                (methodCallExpression.Method.Name == nameof(QueryExtensions.DataSet) &&
                                methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
                            )
                    ) ||
                    (node is ConstantExpression constExpression && constExpression.Value is IQueryable);
        }

        public virtual bool IsChainedQueryMethod(Expression currentNode, Expression parentNode)
        {
            if (IsQueryMethod(currentNode) && IsQueryMethod(parentNode))
            {
                if (parentNode is MethodCallExpression methodCallExpression &&
                    methodCallExpression.Arguments.FirstOrDefault() == currentNode)
                    return true;
                else if (parentNode is ChainedQueryExpression chainedQueryExpression &&
                    chainedQueryExpression.Query == currentNode)
                    return true;
            }
            return false;
        }

        public virtual bool IsQueryMethod(Expression node)
        {
            return (
                    node is MethodCallExpression methodCallExpression &&
                    (methodCallExpression.Method.DeclaringType == typeof(Queryable) ||
                    methodCallExpression.Method.DeclaringType == typeof(Enumerable) ||
                    this.IsQueryExtensionMethod(methodCallExpression))
                    )
                    ||
                    node is ChainedQueryExpression;
        }

        private bool IsQueryExtensionMethod(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.DeclaringType == typeof(QueryExtensions)
                    &&
                    !(methodCallExpression.Method.Name == nameof(QueryExtensions.Schema))
                    ;
        }
    }
}
