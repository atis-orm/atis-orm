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
    ///         Factory class for creating <see cref="ParameterExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class ParameterExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<ParameterExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ParameterExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public ParameterExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <summary>
        ///     <para>
        ///         Attempts to create an expression converter for the specified source expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">The source expression for which the converter is being created.</param>
        /// <param name="converterStack">The current stack of converters in use, which may influence the creation of the new converter.</param>
        /// <param name="converter">When this method returns, contains the created expression converter if the creation was successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a suitable converter was successfully created; otherwise, <c>false</c>.</returns>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is ParameterExpression parameterExpression)
            {
                converter = new ParameterExpressionConverter(this.Context, parameterExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts <see cref="ParameterExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class ParameterExpressionConverter : LinqToNonSqlQueryConverterBase<ParameterExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterMapper;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ParameterExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ParameterExpressionConverter(IConversionContext context, ParameterExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.parameterMapper = context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether the current parameter expression is a leaf node in the expression tree.
        ///     </para>
        /// </summary>
        protected virtual bool IsLefNode => !(this.ParentConverter is MemberExpressionConverter);

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sqlExpression = this.parameterMapper.GetDataSourceByParameterExpression(this.Expression)
                                    ??
                                    throw new InvalidOperationException($"No Parameter Mapping found for ParameterExpression '{this.Expression}'.");

            if (sqlExpression is SqlQueryShapeFieldResolverExpression fieldResolver)
            {
                if (fieldResolver.IsScalar)
                    return fieldResolver.GetScalarExpression();
                else if (this.IsLefNode)
                    // if parameter is selected alone then we don't want to return the SqlQueryShapeFieldResolverExpression
                    // as it would be difficult to implement everywhere conditions to handle this expression
                    return fieldResolver.ShapeExpression;
            }

            return sqlExpression;
        }
    }
}
