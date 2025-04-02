using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.Abstractions
{
    /// <summary>
    ///     <para>
    ///         Interface for evaluating expressions.
    ///     </para>
    /// </summary>
    public interface IExpressionEvaluator
    {
        /// <summary>
        ///     <para>
        ///         Evaluates the given expression and returns the result.
        ///     </para>
        /// </summary>
        /// <param name="expression">
        ///     <para>
        ///         The expression to evaluate.
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para>
        ///         The result of the evaluated expression.
        ///     </para>
        /// </returns>
        object Eval(Expression expression);
    }
}
