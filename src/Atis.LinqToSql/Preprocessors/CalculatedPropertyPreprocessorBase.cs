using Atis.Expressions;
using System;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Abstract base class for preprocessing calculated properties in expressions.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Atis LinqToSql abstracts the implementation details of calculated properties.
    ///         It is the responsibility of the consumer to implement this class and define
    ///         how calculated properties will be identified and processed.
    ///     </para>
    /// </remarks>
    public abstract class CalculatedPropertyPreprocessorBase : IExpressionPreprocessor
    {
        /// <inheritdoc/>
        public void BeforeVisit(Expression node, Expression[] expressionsStack)
        {
            // do nothing
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc/>
        public Expression Preprocess(Expression node, Expression[] expressionsStack)
        {
            if (node is MemberExpression memberExpression &&
                this.TryGetCalculatedExpression(memberExpression, out LambdaExpression calculatedPropertyExpression))
            {
                if (calculatedPropertyExpression.Parameters.Count == 0)
                    throw new InvalidOperationException($"Preprocessing expression '{node}' for calculated property, but returned LambdaExpression does not have any parameters.");
                try
                {
                    return ExpressionReplacementVisitor.Replace(calculatedPropertyExpression.Parameters[0], memberExpression.Expression, calculatedPropertyExpression.Body);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"An error occurred while extracting the expression for the calculated property node '{node}', see inner exception for details.", ex);
                }
            }
            return node;
        }

        /// <summary>
        ///     <para>
        ///         Tries to get the calculated expression for a given member expression.
        ///     </para>
        /// </summary>
        /// <param name="memberExpression">The member expression to evaluate.</param>
        /// <param name="calculatedPropertyExpression">The calculated expression if found.</param>
        /// <returns>True if a calculated expression is found; otherwise, false.</returns>
        protected abstract bool TryGetCalculatedExpression(MemberExpression memberExpression, out LambdaExpression calculatedPropertyExpression);
    }
}
