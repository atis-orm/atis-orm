using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.Expressions
{
    /// <summary>
    /// Abstract base class for converting expression trees from source expressions to destination expressions.
    /// </summary>
    /// <typeparam name="TSourceExpression">The type of the source expression to be converted.</typeparam>
    /// <typeparam name="TDestinationExpression">The type of the destination expression produced by the conversion.</typeparam>
    public abstract class ExpressionConverterBase<TSourceExpression, TDestinationExpression>
        where TSourceExpression : class
        where TDestinationExpression : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionConverterBase{TSourceExpression, TDestinationExpression}"/> class.
        /// </summary>
        /// <param name="expression">The source expression that will be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ExpressionConverterBase(TSourceExpression expression, ExpressionConverterBase<TSourceExpression, TDestinationExpression>[] converterStack)
        {
            this.Expression = expression;
            this.ConverterStack = converterStack;
        }

        /// <summary>
        /// Gets the stack of converters representing the context hierarchy of the current conversion.
        /// </summary>
        public virtual ExpressionConverterBase<TSourceExpression, TDestinationExpression>[] ConverterStack { get; }

        /// <summary>
        /// Gets the source expression that is currently being converted.
        /// </summary>
        public virtual TSourceExpression Expression { get; }

        /// <summary>
        /// Gets the parent expression in the conversion stack, if available.
        /// </summary>
        public virtual TSourceExpression ParentExpression => this.ParentConverter?.Expression;

        /// <summary>
        ///     <para>
        ///         Gets the parent converter in the stack, providing context during the conversion process.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This property is useful for determining the parent of the current expression, allowing child converters to adapt their behavior 
        ///         based on the parent's context. For example, it can be used to differentiate between intermediate nodes and leaf nodes in an expression chain.
        ///     </para>
        /// </remarks>
        public virtual ExpressionConverterBase<TSourceExpression, TDestinationExpression> ParentConverter => this.ConverterStack?.FirstOrDefault();

        /// <summary>
        /// Converts the source expression to the destination expression.
        /// </summary>
        /// <param name="convertedChildren">Children expressions that are already converted and required for this converter.</param>
        /// <returns>The converted destination expression.</returns>
        public abstract TDestinationExpression Convert(TDestinationExpression[] convertedChildren);

        /// <summary>
        /// Method called before visiting the source expression. Allows for pre-processing or preparation.
        /// </summary>
        public virtual void OnBeforeVisit()
        {
            // No default action
        }

        /// <summary>
        /// Method called after visiting the source expression. Allows for any post-processing steps.
        /// </summary>
        public virtual void OnAfterVisit()
        {
            // No default action
        }

        /// <summary>
        /// Method called before visiting a child expression. Allows customization of pre-processing specific child nodes.
        /// </summary>
        /// <param name="childNode">The child node about to be visited.</param>
        public virtual void OnBeforeChildVisit(TSourceExpression childNode)
        {
            // No default action
        }

        /// <summary>
        /// Method called when a child expression has been successfully converted.
        /// </summary>
        /// <param name="childConverter">The converter responsible for converting the child expression.</param>
        /// <param name="childNode">The original child source expression.</param>
        /// <param name="convertedExpression">The converted destination expression of the child node.</param>
        public virtual void OnConversionCompletedByChild(ExpressionConverterBase<TSourceExpression, TDestinationExpression> childConverter, TSourceExpression childNode, TDestinationExpression convertedExpression)
        {
            // No default action
        }

        /// <summary>
        /// Tries to override the conversion of a given child expression.
        /// </summary>
        /// <param name="sourceExpression">The source expression of the child to be potentially overridden.</param>
        /// <param name="convertedExpression">The overridden converted expression, if successful.</param>
        /// <returns><c>true</c> if the conversion was overridden successfully; otherwise, <c>false</c>.</returns>
        public virtual bool TryOverrideChildConversion(TSourceExpression sourceExpression, out TDestinationExpression convertedExpression)
        {
            convertedExpression = null;
            return false;
        }

        public virtual bool TryCreateChildConverter(TSourceExpression childNode, ExpressionConverterBase<TSourceExpression, TDestinationExpression>[] converterStack, out ExpressionConverterBase<TSourceExpression, TDestinationExpression> childConverter)
        {
            childConverter = null;
            return false;
        }
    }

}
