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
    public interface IExpressionPreprocessor
    {
        /// <summary>
        ///     <para>
        ///         Initializes the Expression Preprocessor instance.
        ///     </para>
        /// </summary>
        void Initialize();

        /// <summary>
        ///     <para>
        ///         Processes and potentially transforms the given expression node based on 
        ///         custom preprocessing logic.
        ///     </para>
        /// </summary>
        /// <param name="node">The current expression node to preprocess.</param>
        /// <returns>The transformed or unmodified expression.</returns>
        Expression Preprocess(Expression node);
    }

}
