using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{
    /// <summary>
    /// A visitor that replaces specified expressions within an expression tree.
    /// </summary>
    public class ExpressionReplacementVisitor : ExpressionVisitor
    {
        private readonly IReadOnlyList<Expression> _originals;
        private readonly IReadOnlyList<Expression> _replacements;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionReplacementVisitor"/> class.
        /// </summary>
        /// <param name="originals">The list of original expressions to be replaced.</param>
        /// <param name="replacements">The list of replacement expressions.</param>
        /// <exception cref="ArgumentException">Thrown when the number of originals and replacements do not match.</exception>
        public ExpressionReplacementVisitor(IReadOnlyList<Expression> originals, IReadOnlyList<Expression> replacements)
        {
            if (originals.Count != replacements.Count)
                throw new ArgumentException("Originals and replacements must have the same number of elements.");

            _originals = originals;
            _replacements = replacements;
        }

        /// <summary>
        /// Replaces a single expression within an expression tree.
        /// </summary>
        /// <param name="original">The original expression to be replaced.</param>
        /// <param name="replacement">The replacement expression.</param>
        /// <param name="tree">The expression tree where the replacement will occur.</param>
        /// <returns>The modified expression tree.</returns>
        public static Expression Replace(Expression original, Expression replacement, Expression tree)
            => new ExpressionReplacementVisitor(new[] { original }, new[] { replacement }).Visit(tree);

        /// <summary>
        /// Replaces multiple expressions within an expression tree.
        /// </summary>
        /// <param name="originals">The list of original expressions to be replaced.</param>
        /// <param name="replacements">The list of replacement expressions.</param>
        /// <param name="tree">The expression tree where the replacements will occur.</param>
        /// <returns>The modified expression tree.</returns>
        public static Expression Replace(IReadOnlyList<Expression> originals, IReadOnlyList<Expression> replacements, Expression tree)
            => new ExpressionReplacementVisitor(originals, replacements).Visit(tree);

        /// <summary>
        /// Visits the <see cref="Expression"/> and replaces it if it matches any of the original expressions.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression if a match is found; otherwise, the original expression.</returns>
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;

            for (var i = 0; i < _originals.Count; i++)
            {
                if (node.Equals(_originals[i]))
                {
                    return _replacements[i];
                }
            }

            return base.Visit(node);
        }
    }
}
