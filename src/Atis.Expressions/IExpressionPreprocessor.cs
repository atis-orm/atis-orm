using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{
    /// <summary>
    ///     <para>
    ///         Represents an expression preprocessor that modifies or replaces a given expression node 
    ///         during the preprocessing phase of expression tree traversal.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This interface is used by the <see cref="PreprocessingExpressionVisitor"/> class, which iterates
    ///         through the entire expression tree and passes each node to different implementations of 
    ///         <c>IExpressionPreprocessor</c>. 
    ///     </para>
    ///     <para>
    ///         Each implementation of <c>IExpressionPreprocessor</c> is responsible for handling a 
    ///         single node at a time without recursively traversing the expression tree, as the parent 
    ///         visitor handles the traversal.
    ///     </para>
    /// </remarks>
    public interface IExpressionPreprocessor
    {
        void BeforeVisit(Expression node, Expression[] expressionsStack);
        void Initialize();

        /// <summary>
        ///     <para>
        ///         Processes and potentially transforms the given expression node based on 
        ///         custom preprocessing logic.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The preprocessing operation should focus on modifying or replacing the given node 
        ///         without further traversing the expression tree, as the <c>PreprocessingExpressionVisitor</c>
        ///         already performs the full traversal.
        ///     </para>
        ///     <para>
        ///         The <paramref name="expressionsStack"/> provides contextual expressions that might 
        ///         be useful for preprocessing decisions, such as tracking parent expressions or ensuring 
        ///         proper transformations based on surrounding expressions.
        ///     </para>
        /// </remarks>
        /// <param name="node">The current expression node to preprocess.</param>
        /// <param name="expressionsStack">
        /// A stack containing parent expressions leading up to the current <paramref name="node"/>.
        /// This can be used to provide additional context during preprocessing.
        /// </param>
        /// <returns>The transformed or unmodified expression.</returns>
        Expression Preprocess(Expression node, Expression[] expressionsStack);
    }

}
