using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating instances of <see cref="SubQueryNavigationExpressionConverter"/>.
    ///     </para>
    /// </summary>
    public class SubQueryNavigationExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<SubQueryNavigationExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SubQueryNavigationExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public SubQueryNavigationExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is SubQueryNavigationExpression subQueryNavigationExpression)
            {
                converter = new SubQueryNavigationExpressionConverter(Context, subQueryNavigationExpression, converterStack);
                return true;
            }
            else
            {
                converter = null;
                return false;
            }
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting <see cref="SubQueryNavigationExpression"/> to <see cref="SqlExpression"/>.
    ///     </para>
    /// </summary>
    public class SubQueryNavigationExpressionConverter : LinqToSqlQueryConverterBase<SubQueryNavigationExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SubQueryNavigationExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converters">The stack of converters representing the parent chain for context-aware conversion.</param>
        public SubQueryNavigationExpressionConverter(IConversionContext context, SubQueryNavigationExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sqlQuery = convertedChildren[0] as SqlSelectExpression
                            ??
                            throw new InvalidOperationException($"Expected a {nameof(SqlSelectExpression)} but got {convertedChildren[0].GetType().Name}.");
            sqlQuery.Tag = this.Expression.NavigationProperty;
            return sqlQuery;
        }

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Query;
    }
}
