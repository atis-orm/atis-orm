using Atis.Expressions;
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;

namespace Atis.LinqToSql.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Represents a preprocessor that replaces query variables in an expression tree with their corresponding expressions.
    ///     </para>
    /// </summary>
    public class QueryVariableReplacementPreprocessor : IExpressionPreprocessor
    {
        /// <summary>
        ///     <para>
        ///         Determines whether the specified type is a query type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the specified type is a query type; otherwise, <c>false</c>.</returns>
        protected virtual bool IsQueryType(Type type)
        {
            return typeof(IQueryable).IsAssignableFrom(type);
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node, Expression[] expressionsStack)
        {
            if (this.IsQueryType(node.Type) &&
                node is MemberExpression memberExpr &&
                memberExpr.Expression is ConstantExpression constExpr &&
                constExpr.Value != null)
            {
                var propInfo = memberExpr.Member as PropertyInfo;
                if (propInfo != null)
                    return (propInfo.GetValue(constExpr.Value) as IQueryable)?.Expression
                        ?? throw new InvalidOperationException($"Property {propInfo.Name} is not initialized or is not of type {typeof(IQueryable)}");
                var fieldInfo = memberExpr.Member as FieldInfo;
                if (fieldInfo != null)
                    return (fieldInfo.GetValue(constExpr.Value) as IQueryable)?.Expression
                        ?? throw new InvalidOperationException($"Field {fieldInfo.Name} is not initialized or is not of type {typeof(IQueryable)}");
                throw new InvalidOperationException($"Member {memberExpr.Member.Name} is not a property or field");
            }
            else if (this.IsQueryType(node.Type) &&
                     node is ConstantExpression constExpr2 &&
                     constExpr2.Value is IQueryable q &&
                     q.Expression == node)
            {
                return Expression.Call(typeof(QueryExtensions), nameof(QueryExtensions.DataSet), new Type[] { node.Type.GetGenericArguments().First() }, Expression.Constant(q.Provider));
            }
            return node;
        }

        /// <inheritdoc />
        public void BeforeVisit(Expression node, Expression[] expressionsStack)
        {
            // do nothing
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }
    }
}
