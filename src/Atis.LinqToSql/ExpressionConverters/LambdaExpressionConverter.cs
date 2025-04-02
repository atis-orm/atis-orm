using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters that handle LambdaExpression instances.
    ///     </para>
    /// </summary>
    public class LambdaExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<LambdaExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="LambdaExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public LambdaExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is LambdaExpression lambdaExpression)
            {
                converter = new LambdaExpressionConverter(this.Context, lambdaExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting LambdaExpression instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class LambdaExpressionConverter : LinqToSqlExpressionConverterBase<LambdaExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="LambdaExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The LambdaExpression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public LambdaExpressionConverter(IConversionContext context, LambdaExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // only interested in body
            var bodyString = convertedChildren[0];
            // convertedChildren[1..N] are parameters and we don't need them
            return bodyString;
        }
    }
}
