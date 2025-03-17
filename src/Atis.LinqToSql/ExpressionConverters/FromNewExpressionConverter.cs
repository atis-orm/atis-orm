using Atis.Expressions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    // Note: we don't have factory for this converter because these converters are initialized
    // by the parent converter which is FormQueryMethodExpressionConverter.

    /// <summary>
    ///     <para>
    ///         Converter class for handling <see cref="QueryExtensions.From{T}(IQueryProvider, Expression{Func{T}})"/> method call
    ///         having <see cref="NewExpression"/> as parameter.
    ///     </para>
    /// </summary>
    public class FromNewExpressionConverter : FromSourceExpressionConverterBase<NewExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="FromNewExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The <see cref="NewExpression"/> to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public FromNewExpressionConverter(IConversionContext context, NewExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
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
