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
    ///         Converter for converting MemberInitExpression used in conjunction with <see cref="QueryExtensions.From{T}(IQueryProvider, Expression{Func{T}})"/> 
    ///         method call, to SQL expressions.
    ///     </para>
    /// </summary>
    public class FromMemberInitExpressionConverter : FromSourceExpressionConverterBase<MemberInitExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="FromMemberInitExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The MemberInitExpression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public FromMemberInitExpressionConverter(IConversionContext context, MemberInitExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override string[] GetMemberNames()
        {
            return this.Expression.Bindings.Select(x => x.Member.Name).ToArray();
        }

        /// <inheritdoc />
        protected override SqlExpression[] GetSqlExpressions(SqlExpression[] convertedChildren)
        {
            var skipCount = 0;
            if (this.Expression.NewExpression != null)
                skipCount = 1;
            var convertedArray = convertedChildren.Skip(skipCount).Take(this.Expression.Bindings.Count).ToArray();
            return convertedArray;
        }
    }
}
