using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{

    /// <summary>
    /// This class represents a custom ExpressionVisitor that applies multiple preprocessors 
    /// to an expression tree. Each preprocessor operates on the entire tree in sequence.
    /// </summary>
    public class PreprocessingExpressionVisitor : ExpressionVisitor
    {
        // A list of preprocessors that will be applied to the expression tree.
        private readonly IList<IExpressionPreprocessor> _preprocessors;

        // Maximum number of iterations allowed to apply preprocessors to the expression tree.
        private readonly int _maxIterations;

        // A stack to store the expression nodes being visited, helping track the current position in the tree.
        private readonly Stack<Expression> expressionStacks = new Stack<Expression>();

        // The currently active preprocessor that is operating on the expression tree.
        private IExpressionPreprocessor currentPreprocess;

        /// <summary>
        /// Initializes a new instance of the PreprocessingExpressionVisitor class.
        /// </summary>
        /// <param name="preprocessors">A collection of preprocessors that will be applied to the expression tree.</param>
        /// <param name="maxIterations">The maximum number of iterations to apply the preprocessors. Defaults to 50.</param>
        /// <exception cref="ArgumentNullException">Thrown if the collection of preprocessors is null or empty.</exception>
        public PreprocessingExpressionVisitor(IEnumerable<IExpressionPreprocessor> preprocessors, int maxIterations = 50)
        {
            if (preprocessors is null || !preprocessors.Any())
                throw new ArgumentNullException(nameof(preprocessors));

            _preprocessors = preprocessors.ToList();
            _maxIterations = maxIterations;
        }

        /// <summary>
        /// Preprocesses the expression tree by applying each preprocessor in sequence, 
        /// repeating the process until no further changes occur or the maximum iterations are reached.
        /// </summary>
        /// <param name="node">The expression tree to preprocess.</param>
        /// <returns>The preprocessed expression tree.</returns>
        /// <exception cref="PreprocessingThresholdExceededException">
        /// Thrown if the number of iterations exceeds the maximum allowed.
        /// </exception>
        public Expression Preprocess(Expression node)
        {
            // Flag to indicate whether the expression tree has been modified.
            bool expressionChanged;
            // Counter to track the number of iterations performed.
            int iterations = 0;

            do
            {
                expressionChanged = false;

                // Loop through each preprocessor, applying it to the entire expression tree.
                foreach (var preprocessor in _preprocessors)
                {
                    // Set the current preprocessor to be used during the tree traversal.
                    this.currentPreprocess = preprocessor;
                    this.currentPreprocess.Initialize();

                    // Visit the entire expression tree using the current preprocessor.
                    var newNode = this.Visit(node);

                    // Check if the preprocessor made changes to the tree.
                    if (newNode != node)
                    {
                        node = newNode;
                        expressionChanged = true;
                    }
                }

                iterations++;

                // Throw an exception if the maximum iteration limit is reached.
                if (iterations >= _maxIterations)
                {
                    throw new PreprocessingThresholdExceededException(this._maxIterations);
                }

            } while (expressionChanged); // Continue preprocessing until no more changes are made.

            return node; // Return the fully preprocessed expression tree.
        }

        /// <summary>
        /// Visits each node in the expression tree and applies the current preprocessor.
        /// </summary>
        /// <param name="node">The expression node being visited.</param>
        /// <returns>The modified expression node after applying the current preprocessor.</returns>
        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;

            // Push the current expression onto the stack to track the current traversal state.
            this.expressionStacks.Push(node);

            try
            {
                var expressionsStackArray = this.expressionStacks.ToArray();
                
                // TODO: decide if we want to keep this
                this.currentPreprocess.BeforeVisit(node, expressionsStackArray);

                // Traverse the children of the current node.
                node = base.Visit(node);

                // Apply the current preprocessor to the current node.
                node = this.currentPreprocess.Preprocess(node, expressionsStackArray);

                return node;
            }
            finally
            {
                // Pop the current node from the stack after it's processed.
                this.expressionStacks.Pop();
            }
        }
    }
}
