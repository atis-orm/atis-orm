using System;
using System.Linq.Expressions;

namespace Atis.Expressions
{
    /// <summary>
    /// ExpressionFinder is a visitor that searches for a specific expression within an expression tree.
    /// </summary>
    public class ExpressionFinder : ExpressionVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionFinder"/> class with the target expression to search for.
        /// </summary>
        /// <param name="target">The target expression to search for.</param>
        /// <exception cref="ArgumentNullException">Thrown when the target expression is null.</exception>
        public ExpressionFinder(Expression target)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>
        /// Gets the target expression to search for.
        /// </summary>
        public Expression Target { get; }

        /// <summary>
        /// Gets a value indicating whether the target expression was found in the expression tree.
        /// </summary>
        public bool Found { get; private set; }

        /// <summary>
        /// Searches for the target expression within the specified expression tree.
        /// </summary>
        /// <param name="node">The root of the expression tree to search.</param>
        /// <returns><c>true</c> if the target expression is found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided expression tree is null.</exception>
        public bool FindIn(Expression node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            this.Found = false;
            this.Visit(node);
            return this.Found;
        }

        /// <summary>
        /// Visits the given expression and checks if it matches the target expression.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The visited expression.</returns>
        public override Expression Visit(Expression node)
        {
            if (node == this.Target)
            {
                this.Found = true;
                return node; // Early exit: no need to visit further
            }
            return base.Visit(node);
        }
    }

}
