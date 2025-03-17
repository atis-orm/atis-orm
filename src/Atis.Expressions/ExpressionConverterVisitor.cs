using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{
    /// <summary>
    /// Represents a visitor that traverses source expressions and converts them to destination expressions
    /// using an <see cref="ExpressionConverterProvider{TSourceExpression, TDestinationExpression}"/>.
    /// Implements a read-only stack to manage converted expressions during the traversal.
    /// </summary>
    /// <remarks>
    /// This class does not itself provide the functionality of an expression visitor. 
    /// Instead, it is intended to be used within a well-defined expression visitor class, 
    /// such as LINQ's <see cref="System.Linq.Expressions.ExpressionVisitor"/>. 
    /// The consumer of this class must call the <see cref="Visit"/> method within the actual visitor's <c>Visit</c> method
    /// to integrate conversion logic into the traversal.
    /// </remarks>
    /// <typeparam name="TSourceExpression">The type of the source expression.</typeparam>
    /// <typeparam name="TDestinationExpression">The type of the destination expression.</typeparam>
    public class ExpressionConverterVisitor<TSourceExpression, TDestinationExpression>
        where TSourceExpression : class
        where TDestinationExpression : class
    {
        /// <summary>
        /// Gets the stack of converted expressions used during the traversal.
        /// </summary>
        protected virtual Stack<TDestinationExpression> ConvertedExpressionStack { get; } = new Stack<TDestinationExpression>();

        /// <summary>
        /// Gets the <see cref="IExpressionConverterProvider{TSourceExpression, TDestinationExpression}"/> responsible
        /// for managing the conversion process.
        /// </summary>
        public virtual IExpressionConverterProvider<TSourceExpression, TDestinationExpression> ConverterProvider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionConverterVisitor{TSourceExpression, TDestinationExpression}"/> class
        /// with the specified converter provider.
        /// </summary>
        /// <param name="converterProvider">
        /// The <see cref="IExpressionConverterProvider{TSourceExpression, TDestinationExpression}"/> to use for expression conversion.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="converterProvider"/> is <c>null</c>.</exception>
        public ExpressionConverterVisitor(IExpressionConverterProvider<TSourceExpression, TDestinationExpression> converterProvider)
        {
            this.ConverterProvider = converterProvider ?? throw new ArgumentNullException(nameof(converterProvider));
        }

        /// <summary>
        /// Retrieves and removes the most recently converted expression from the stack.
        /// </summary>
        /// <returns>The most recently converted expression.</returns>
        public TDestinationExpression GetConvertedExpression() => this.ConvertedExpressionStack.Pop();

        public TDestinationExpression LastConversion => this.ConvertedExpressionStack.Count > 0 ? this.ConvertedExpressionStack.Peek() : null;

        /// <summary>
        /// Clears the stack.
        /// </summary>
        public void Initialize() => this.ConvertedExpressionStack.Clear();

        /// <summary>
        /// Visits the specified source expression, applies the base visitor, and converts the expression if applicable.
        /// </summary>
        /// <param name="node">The source expression to visit.</param>
        /// <param name="baseVisit">The base visitor function to apply to the expression.</param>
        /// <returns>The visited source expression.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseVisit"/> is <c>null</c>.</exception>
        public virtual TSourceExpression Visit(TSourceExpression node, Func<TSourceExpression, TSourceExpression> baseVisit)
        {
            if (baseVisit is null)
                throw new ArgumentNullException(nameof(baseVisit));

            if (node == null) return null;

            // Attempt to override the child node conversion
            if (this.ConverterProvider.TryOverrideChildConversion(node, out var convertedChild))
            {
                this.ConvertedExpressionStack.Push(convertedChild);
                return node;
            }

            // Notify the provider to prepare for visiting the node
            this.ConverterProvider.OnBeforeVisit(node);

            // Visit the node using the base visitor
            var beforeConversionCount = this.ConvertedExpressionStack.Count;
            var visitedExpression = baseVisit(node);
            if (visitedExpression != null)
            {
                // Convert the visited expression and push it onto the stack
                var convertedChildren = this.PopChildren(beforeConversionCount);
                var convertedExpression = this.ConverterProvider.Convert(visitedExpression, convertedChildren);
                if (this.ConvertedExpressionStack.Count != beforeConversionCount)
                {
                    throw new InvalidOperationException($"Number of expected converted expressions on stack are not matching for Node Type '{visitedExpression.GetType().Name}'. This can happen because the number expressions are not correctly being popped in the converter.");
                }
                this.ConvertedExpressionStack.Push(convertedExpression);
            }
            return visitedExpression;
        }

        private TDestinationExpression[] PopChildren(int stackCountBeforeVisit)
        {
            return Enumerable.Range(0, this.ConvertedExpressionStack.Count - stackCountBeforeVisit)
                        .Select(_ => this.ConvertedExpressionStack.Pop())
                        .Reverse()
                        .ToArray();
        }
    }

}
