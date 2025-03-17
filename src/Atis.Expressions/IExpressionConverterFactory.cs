using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.Expressions
{
    /// <summary>
    /// Defines the contract for a factory that creates expression converters for transforming
    /// source expressions to destination expressions.
    /// </summary>
    /// <typeparam name="TSource">The type of the source expression.</typeparam>
    /// <typeparam name="TDestination">The type of the destination expression.</typeparam>
    public interface IExpressionConverterFactory<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
        /// <summary>
        /// Attempts to create an expression converter for the specified source expression.
        /// </summary>
        /// <param name="expression">The source expression for which the converter is being created.</param>
        /// <param name="convertersStack">
        /// The current stack of converters in use, which may influence the creation of the new converter.
        /// </param>
        /// <param name="converter">
        /// When this method returns, contains the created expression converter if the creation was successful; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a suitable converter was successfully created; otherwise, <c>false</c>.
        /// </returns>
        bool TryCreate(TSource expression, ExpressionConverterBase<TSource, TDestination>[] convertersStack, out ExpressionConverterBase<TSource, TDestination> converter);
    }

}
