using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionExtensions
{
    /// <summary>
    ///     <para>
    ///         Represents an abstract base class for expressions that represents a chained query.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Usually the method calls are chained together to form a query. Therefore, 
    ///         this class provides a mechanism so that custom Expression classes defined by
    ///         developers can also behave in the same way.
    ///     </para>
    ///     <para>
    ///         During transformation process, this is important for internal classes to
    ///         determine if the given expression is a continued call on the same query, 
    ///         therefore, custom Expression classes which are not <c>MethodCallExpression</c>
    ///         can inherit from this class so that the normalization process will consider
    ///         them as continued query method calls.
    ///     </para>
    /// </remarks>
    public abstract class ChainedQueryExpression : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ChainedQueryExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="queryExpression">The query expression to be chained.</param>
        public ChainedQueryExpression(Expression queryExpression)
        {
            Query = queryExpression;
            this.Type = Query.Type;
        }

        /// <summary>
        ///     <para>
        ///         Gets the query expression.
        ///     </para>
        /// </summary>
        public Expression Query { get; }

        /// <summary>
        ///     <para>
        ///         Gets the node type of this expression.
        ///     </para>
        /// </summary>
        public sealed override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        ///     <para>
        ///         Gets the static type of the expression that this <see cref="Expression"/> represents.
        ///     </para>
        /// </summary>
        public sealed override Type Type { get; }
    }
}
