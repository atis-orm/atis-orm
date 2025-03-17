using Atis.Expressions;
using Atis.LinqToSql.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating <see cref="QuoteExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class QuoteExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<UnaryExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="QuoteExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public QuoteExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Quote)
            {
                converter = new QuoteExpressionConverter(this.Context, unaryExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for handling <see cref="UnaryExpression"/> nodes with <see cref="ExpressionType.Quote"/>.
    ///     </para>
    /// </summary>
    public class QuoteExpressionConverter : LinqToSqlExpressionConverterBase<UnaryExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="QuoteExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public QuoteExpressionConverter(IConversionContext context, UnaryExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return convertedChildren[0];
        }
    }
}
