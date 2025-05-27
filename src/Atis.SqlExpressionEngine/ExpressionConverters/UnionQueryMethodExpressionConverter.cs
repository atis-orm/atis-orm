using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters for Union query methods.
    ///     </para>
    /// </summary>
    public class UnionQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="UnionQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public UnionQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (
                expression is MethodCallExpression methodCallExpression &&
                (
                    (methodCallExpression.Method.Name == nameof(Queryable.Union) &&
                    (methodCallExpression.Method.DeclaringType == typeof(Queryable) ||
                    methodCallExpression.Method.DeclaringType == typeof(Enumerable)))
                    ||
                    (methodCallExpression.Method.Name == nameof(QueryExtensions.UnionAll) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
                )
            )
            {
                converter = new UnionQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for Union query method expressions.
    ///     </para>
    /// </summary>
    public class UnionQueryMethodExpressionConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="UnionQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public UnionQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            if (convertedChildren.Length != 2)
                throw new ArgumentException("Union method must have exactly two arguments.");

            var firstUnionItems = GetUnionItems(convertedChildren[0], "first");
            var secondUnionItems = GetUnionItems(convertedChildren[1], "second");

            return this.SqlFactory.CreateUnionQuery(firstUnionItems.Concat(secondUnionItems).ToArray());
        }

        private UnionItem[] GetUnionItems(SqlExpression convertedExpression, string argumentNumber)
        {
            var unionType = this.Expression.Method.Name == nameof(QueryExtensions.UnionAll) ? SqlUnionType.UnionAll : SqlUnionType.Union;

            UnionItem[] unionItems;
            if (convertedExpression is SqlDerivedTableExpression derivedTable)
            {
                var secondUnionItem = new UnionItem(derivedTable, unionType);
                unionItems = new[] { secondUnionItem };
            }
            else if (convertedExpression is SqlUnionQueryExpression unionQuery)
            {
                unionItems = unionQuery.Unions.ToArray();
                // The unions coming from a union query might have a different union type
                // than the first union. When combining both in this union, we need to
                // ensure the union type provided in this method is applied consistently.
                unionItems[0] = new UnionItem(unionItems[0].DerivedTable, unionType);
            }
            else
            {
                string furtherMessage;
                if (convertedExpression is SqlSelectExpression)
                    furtherMessage = $" This could happen because the {nameof(SqlSelectExpression)} is not being closed and converted to {nameof(SqlDerivedTableExpression)}.";
                else
                    furtherMessage = string.Empty;
                throw new InvalidOperationException($"Expected type of {argumentNumber} argument of union is either '{nameof(SqlDerivedTableExpression)}' or '{nameof(SqlUnionQueryExpression)}' while we received '{convertedExpression.GetType()}' type.{furtherMessage}");
            }
            return unionItems;
        }
    }
}
