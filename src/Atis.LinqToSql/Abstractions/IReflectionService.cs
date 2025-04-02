using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.LinqToSql.Abstractions
{
    /// <summary>
    ///     <para>
    ///         Provides methods for reflection-based operations.
    ///     </para>
    /// </summary>
    public interface IReflectionService
    {
        /// <summary>
        ///     <para>
        ///         Creates an instance of the specified type using the provided constructor arguments.
        ///     </para>
        /// </summary>
        /// <param name="type">The type to create an instance of.</param>
        /// <param name="ctorArgs">The constructor arguments.</param>
        /// <returns>The created instance.</returns>
        object CreateInstance(Type type, object[] ctorArgs);

        /// <summary>
        ///     <para>
        ///         Gets the entity type from a queryable type.
        ///     </para>
        /// </summary>
        /// <param name="queryableType">The queryable type.</param>
        /// <returns>The entity type.</returns>
        Type GetEntityTypeFromQueryableType(Type queryableType);

        /// <summary>
        ///     <para>
        ///         Gets the properties of the specified type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type to get properties from.</param>
        /// <returns>An array of PropertyInfo objects.</returns>
        PropertyInfo[] GetProperties(Type type);

        /// <summary>
        ///     <para>
        ///         Gets the property or field information of the specified type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type to get the property or field from.</param>
        /// <param name="propertyOrFieldName">The name of the property or field.</param>
        /// <returns>The MemberInfo object representing the property or field.</returns>
        MemberInfo GetPropertyOrField(Type type, string propertyOrFieldName);

        /// <summary>
        ///     <para>
        ///         Gets the type of the specified property or field.
        ///     </para>
        /// </summary>
        /// <param name="member">The MemberInfo object representing the property or field.</param>
        /// <returns>The type of the property or field.</returns>
        Type GetPropertyOrFieldType(MemberInfo member);

        /// <summary>
        ///     <para>
        ///         Gets the value of the specified property or field from the given instance.
        ///     </para>
        /// </summary>
        /// <param name="instance">The instance to get the value from.</param>
        /// <param name="propertyOrField">The MemberInfo object representing the property or field.</param>
        /// <returns>The value of the property or field.</returns>
        object GetPropertyOrFieldValue(object instance, MemberInfo propertyOrField);

        /// <summary>
        ///     <para>
        ///         Gets the query expression from a queryable object.
        ///     </para>
        /// </summary>
        /// <param name="queryableObject">The queryable object.</param>
        /// <returns>The query expression.</returns>
        Expression GetQueryExpressionFromQueryable(object queryableObject);

        /// <summary>
        ///     <para>
        ///         Evaluates the specified expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <remarks>
        ///     <para>
        ///         Caution: this method might execute the expression and can have security implications.
        ///     </para>
        /// </remarks>
        object Eval(Expression expression);

        /// <summary>
        ///     <para>
        ///         Determines whether the specified type is a primitive type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a primitive type; otherwise, false.</returns>
        bool IsPrimitiveType(Type type);

        /// <summary>
        ///     <para>
        ///         Determines whether the specified type is a queryable type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a queryable type; otherwise, false.</returns>
        bool IsQueryableType(Type type);

        bool IsQueryMethod(Expression node);

        bool IsQuerySource(Expression node);
        bool IsChainedQueryMethod(Expression currentNode, Expression parentNode);
    }
}
