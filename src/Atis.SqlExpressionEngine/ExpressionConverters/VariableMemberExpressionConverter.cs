using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters that handle variable member expressions.
    ///     </para>
    /// </summary>
    public class VariableMemberExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="VariableMemberExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public VariableMemberExpressionConverterFactory(IConversionContext context) : base(context)
        {
            this.reflectionService = context.GetExtensionRequired<IReflectionService>();
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
            return this.reflectionService.IsVariable(memberExpression);
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
            // Since we are capturing the MemberExpression from the very start, so we don't want to traverse any further
            // e.g. myParam.Prop1.Prop2.Prop3.FinalValue
            // in-above case when visitor will be visiting FinalValue, the factory of this converter will indicate the
            // normalization process that it can handle the MemberExpression and thus the normalization process will
            // use the factory to create the converter for this expression and call will be passed to this converter.
            // While this converter is in place, still the further traversal will happen for children parts of 
            // MemberExpression, that's where below code comes into play. And this Converter effectively tells
            // the visitor that do NOT further traverse the children of this expression by returning true.
            // Which means that, Prop3, Prop2, Prop1 and myParam will not be visited and dummy literal
            // expression will be given to the Convert method of this class.
            // The Convert method will then simply use the IReflectionService to get the value of the expression
            // which usually goes through all the MemberExpression and will reach up to the root object
            // and then get the value of the final property.
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
            return this.reflectionService.Evalulate(memberExpression);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var value = this.GetVariableValue(this.Expression);
            var isEnumerable = this.IsEnumerable(value);
            return this.SqlFactory.CreateParameter(value, multipleValues: isEnumerable);
        }

        private bool IsEnumerable(object value)
        {
            return this.reflectionService.IsEnumerable(value);
        }
    }
}
