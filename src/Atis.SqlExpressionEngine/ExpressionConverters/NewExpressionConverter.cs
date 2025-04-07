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
    ///         Factory for creating <see cref="NewExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class NewExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<NewExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NewExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public NewExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is NewExpression newExpression)
            {
                converter = new NewExpressionConverter(this.Context, newExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for <see cref="NewExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class NewExpressionConverter : CollectiveColumnExpressionConverterBase<NewExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NewExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public NewExpressionConverter(IConversionContext context, NewExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override string[] GetMemberNames()
        {
            return this.Expression.Members?.Select(x => x.Name).ToArray()
                                    ??
                                    throw new InvalidOperationException($"Members of the new expression '{this.Expression}' are not set.");
        }

        /// <inheritdoc />
        protected override SqlExpression[] GetSqlExpressions(SqlExpression[] convertedChildren)
        {
            return convertedChildren.Take(this.Expression.Arguments.Count).ToArray();
        }
    }
}
