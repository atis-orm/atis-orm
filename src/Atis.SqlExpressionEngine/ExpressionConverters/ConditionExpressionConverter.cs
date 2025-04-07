using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating converters that transform conditional expressions from LINQ to SQL.
    ///     </para>
    /// </summary>
    public class ConditionalExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<ConditionalExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ConditionalExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public ConditionalExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is ConditionalExpression conditionExpression)
            {
                converter = new ConditionalExpressionConverter(this.Context, conditionExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for transforming conditional expressions from LINQ to SQL.
    ///     </para>
    /// </summary>
    public class ConditionalExpressionConverter : LinqToSqlExpressionConverterBase<ConditionalExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ConditionalExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The conditional expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ConditionalExpressionConverter(IConversionContext context, ConditionalExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var test = convertedChildren[0];
            var ifTrue = convertedChildren[1];
            var ifFalse = convertedChildren[2];
            var result = this.SqlFactory.CreateCondition(test, ifTrue, ifFalse);
            return result;
        }
    }
}
