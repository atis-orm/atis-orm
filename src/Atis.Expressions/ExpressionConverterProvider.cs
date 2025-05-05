using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.Expressions
{
    /// <summary>
    /// Provides functionality to convert expressions from a source type to a destination type
    /// using a set of factories and a stack of converters.
    /// </summary>
    /// <typeparam name="TSourceExpression">The type of the source expression to convert.</typeparam>
    /// <typeparam name="TDestinationExpression">The type of the destination expression after conversion.</typeparam>
    public class ExpressionConverterProvider<TSourceExpression, TDestinationExpression> : IExpressionConverterProvider<TSourceExpression, TDestinationExpression>
        where TDestinationExpression : class
        where TSourceExpression : class
    {
        /// <summary>
        /// Gets the list of factories used to create expression converters.
        /// </summary>
        protected virtual List<IExpressionConverterFactory<TSourceExpression, TDestinationExpression>> Factories { get; } =
            new List<IExpressionConverterFactory<TSourceExpression, TDestinationExpression>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionConverterProvider{TSourceExpression, TDestinationExpression}"/> class
        /// with the specified factories.
        /// </summary>
        /// <param name="factories">The factories to use for creating expression converters.</param>
        public ExpressionConverterProvider(IEnumerable<IExpressionConverterFactory<TSourceExpression, TDestinationExpression>> factories)
        {
            if (factories != null)
                this.Factories.AddRange(factories);
        }

        /// <summary>
        /// Gets the stack of converters used during the conversion process.
        /// </summary>
        protected virtual Stack<ExpressionConverterBase<TSourceExpression, TDestinationExpression>> ConverterStack { get; } = new Stack<ExpressionConverterBase<TSourceExpression, TDestinationExpression>>();

        
        /// <summary>
        /// Prepares for visiting a source expression by determining the appropriate converter
        /// and pushing it onto the converter stack.
        /// </summary>
        /// <param name="sourceExpression">The source expression to visit.</param>
        /// <exception cref="InvalidOperationException">Thrown if no suitable converter can be found for the given expression.</exception>
        public virtual void OnBeforeVisit(TSourceExpression sourceExpression)
        {
            var converterStackToArray = this.ConverterStack.ToArray();
            var immediateParentConverter = converterStackToArray.FirstOrDefault();
            immediateParentConverter?.OnBeforeChildVisit(sourceExpression);
            ExpressionConverterBase<TSourceExpression, TDestinationExpression> converter = null;
            for (var i = 0; i < converterStackToArray.Length; i++)
            {
                var parentConverter = converterStackToArray[i];
                if (parentConverter.TryCreateChildConverter(sourceExpression, converterStackToArray, out converter))
                {
                    break;
                }
            }
            if (converter is null)
                converter = this.GetConverter(sourceExpression, converterStackToArray)
                                        ?? throw new InvalidOperationException($"No Converter Factory has been defined for Expression '{sourceExpression.GetType()}', '{sourceExpression}'");
            this.ConverterStack.Push(converter);
            converter.OnBeforeVisit();
        }

        private ExpressionConverterBase<TSourceExpression, TDestinationExpression> GetConverter(TSourceExpression sourceExpression, ExpressionConverterBase<TSourceExpression, TDestinationExpression>[] converterStack)
        {
            ExpressionConverterBase<TSourceExpression, TDestinationExpression> converter = null;
            for (var i = 0; i < this.Factories.Count; i++)
            {
                var factory = this.Factories[i];
                if (factory.TryCreate(sourceExpression, converterStack, out converter))
                {
                    break;
                }
            }
            return converter;
        }

        /// <summary>
        /// Gets the current converter at the top of the converter stack.
        /// </summary>
        protected virtual ExpressionConverterBase<TSourceExpression, TDestinationExpression> CurrentConverter => this.ConverterStack.Count > 0 ? this.ConverterStack.Peek() : null;

        /// <inheritdoc />
        public virtual TDestinationExpression Convert(TSourceExpression sourceExpression, TDestinationExpression[] convertedChildren)
        {
            var currentConverter = this.CurrentConverter ?? throw new InvalidOperationException("No current converter found.");
            var convertedValue = currentConverter.CreateFromChildren(convertedChildren);
            this.ConverterStack.Pop();
            var parentConverter = this.CurrentConverter;
            if (parentConverter != null)
            {
                convertedValue = parentConverter.TransformConvertedChild(currentConverter, sourceExpression, convertedValue);
            }
            currentConverter.OnAfterVisit();
            return convertedValue;
        }

        /// <summary>
        /// Attempts to override the conversion of a child node using the current converter.
        /// </summary>
        /// <param name="node">The child node to convert.</param>
        /// <param name="convertedExpression">The converted expression, if overridden successfully.</param>
        /// <returns><c>true</c> if the conversion is overridden; otherwise, <c>false</c>.</returns>
        public virtual bool TryOverrideChildConversion(TSourceExpression node, out TDestinationExpression convertedExpression)
        {
            var parentConverter = this.CurrentConverter;
            if (parentConverter != null)
            {
                if (parentConverter.TryOverrideChildConversion(node, out convertedExpression))
                {
                    convertedExpression = parentConverter.TransformConvertedChild(null, node, convertedExpression);
                    return true;
                }
            }
            convertedExpression = null;
            return false;
        }
    }

}
