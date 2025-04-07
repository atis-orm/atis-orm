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
    ///         Factory class for creating <see cref="MemberInitExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class MemberInitExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberInitExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberInitExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public MemberInitExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberInitExpression memberInitExpression)
            {
                converter = new MemberInitExpressionConverter(this.Context, memberInitExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting <see cref="MemberInitExpression"/> to SQL expressions.
    ///     </para>
    /// </summary>
    public class MemberInitExpressionConverter : CollectiveColumnExpressionConverterBase<MemberInitExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberInitExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The <see cref="MemberInitExpression"/> to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public MemberInitExpressionConverter(IConversionContext context, MemberInitExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            if (sourceExpression == this.Expression.NewExpression)
            {
                convertedExpression = this.SqlFactory.CreateLiteral("dummy");
                return true;
            }
            convertedExpression = null;
            return false;
        }

        /// <inheritdoc />
        protected override string[] GetMemberNames()
        {
            return this.Expression.Bindings?.Select(x => x.Member.Name).ToArray()
                        ??
                        throw new InvalidOperationException($"Bindings of the MemberInitExpression '{this.Expression}' are not set.");
        }

        /// <inheritdoc />
        protected override SqlExpression[] GetSqlExpressions(SqlExpression[] convertedChildren)
        {
            if (this.Expression.Bindings is null)
                throw new InvalidOperationException($"Bindings of the MemberInitExpression '{this.Expression}' are not set.");

            var skipCount = 0;
            if (this.Expression.NewExpression != null)
                skipCount = 1;
            var expressions = convertedChildren.Skip(skipCount).Take(this.Expression.Bindings.Count).ToArray();
            return expressions;
        }
    }
}
