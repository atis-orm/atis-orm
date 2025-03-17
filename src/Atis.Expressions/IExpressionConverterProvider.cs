namespace Atis.Expressions
{
    /// <summary>
    /// Defines the contract for a provider that manages the conversion of expressions
    /// from a source type to a destination type.
    /// </summary>
    /// <typeparam name="TSourceExpression">The type of the source expression to convert.</typeparam>
    /// <typeparam name="TDestinationExpression">The type of the destination expression after conversion.</typeparam>
    public interface IExpressionConverterProvider<TSourceExpression, TDestinationExpression>
        where TSourceExpression : class
        where TDestinationExpression : class
    {
        /// <summary>
        /// Converts a source expression to a destination expression using the current conversion logic.
        /// </summary>
        /// <param name="sourceExpression">The source expression to convert.</param>
        /// <param name="convertedChildren">Children expressions that are already converted and required for this converter.</param>
        /// <returns>The converted destination expression.</returns>
        TDestinationExpression Convert(TSourceExpression sourceExpression, TDestinationExpression[] convertedChildren);

        /// <summary>
        /// Prepares for visiting a source expression by initializing or updating the conversion context.
        /// </summary>
        /// <param name="sourceExpression">The source expression to visit.</param>
        void OnBeforeVisit(TSourceExpression sourceExpression);

        /// <summary>
        /// Attempts to override the conversion of a child node using the current conversion context.
        /// </summary>
        /// <param name="node">The child node to convert.</param>
        /// <param name="convertedExpression">The converted expression, if overridden successfully.</param>
        /// <returns><c>true</c> if the conversion is overridden; otherwise, <c>false</c>.</returns>
        bool TryOverrideChildConversion(TSourceExpression node, out TDestinationExpression convertedExpression);
    }

}