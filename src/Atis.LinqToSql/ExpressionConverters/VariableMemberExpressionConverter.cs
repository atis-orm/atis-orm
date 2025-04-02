using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters that handle variable member expressions.
    ///     </para>
    /// </summary>
    public class VariableMemberExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="VariableMemberExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public VariableMemberExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression memberExpr && IsVariableMemberExpression(memberExpr))
            {
                converter = new VariableMemberExpressionConverter(this.Context, memberExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }

        /// <summary>
        ///     <para>
        ///         Determines whether the specified member expression is a variable member expression.
        ///     </para>
        /// </summary>
        /// <param name="memberExpression">The member expression to check.</param>
        /// <returns><c>true</c> if the specified member expression is a variable member expression; otherwise, <c>false</c>.</returns>
        protected virtual bool IsVariableMemberExpression(MemberExpression memberExpression)
        {
            if (memberExpression.Expression is MemberExpression parentMemberExpr)
            {
                return IsVariableMemberExpression(parentMemberExpr);
            }
            else if (memberExpression.Expression is null || memberExpression.Expression is ConstantExpression)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for handling variable member expressions.
    ///     </para>
    /// </summary>
    public class VariableMemberExpressionConverter : LinqToSqlExpressionConverterBase<MemberExpression>
    {
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="VariableMemberExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The member expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public VariableMemberExpressionConverter(IConversionContext context, MemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.reflectionService = context.GetExtensionRequired<IReflectionService>();
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            convertedExpression = this.SqlFactory.CreateLiteral("dummy");
            return true;
        }

        /// <summary>
        ///     <para>
        ///         Gets the value from the specified member expression.
        ///     </para>
        /// </summary>
        /// <param name="memberExpression">Member expression to get the value from.</param>
        /// <returns>Value from the specified member expression.</returns>
        protected virtual object GetVariableValue(MemberExpression memberExpression)
        {
            return this.reflectionService.Eval(memberExpression);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var value = this.GetVariableValue(this.Expression);
            return this.SqlFactory.CreateParameter(value);
        }
    }
}
