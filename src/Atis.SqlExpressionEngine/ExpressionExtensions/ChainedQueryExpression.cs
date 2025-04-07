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

        ///// <summary>
        /////     <para>
        /////         Creates an updated expression with the specified query expression and updated children.
        /////     </para>
        ///// </summary>
        ///// <param name="queryExpression">The query expression.</param>
        ///// <param name="updatedChildren">The updated children.</param>
        ///// <returns>A new <see cref="ChainedQueryExpression"/> instance.</returns>
        //protected abstract ChainedQueryExpression CreateUpdatedExpression(Expression queryExpression, Expression[] updatedChildren);

        ///// <summary>
        /////     <para>
        /////         Determines whether no children have changed.
        /////     </para>
        ///// </summary>
        ///// <param name="updatedChildren">The updated children.</param>
        ///// <returns><c>true</c> if no children have changed; otherwise, <c>false</c>.</returns>
        ///// <remarks>
        /////     <para>
        /////         The inherited class can check and notify base class whether any children node
        /////         (if any) has been changed. This is useful for the base class to determine if
        /////         it needs to create a new instance or not.
        /////     </para>
        ///// </remarks>
        //protected abstract bool NoChildrenChanged(Expression[] updatedChildren);

        ///// <summary>
        /////     <para>
        /////         Visits the other children of the expression.
        /////     </para>
        ///// </summary>
        ///// <param name="visitor">The expression visitor.</param>
        ///// <returns>An array of visited expressions.</returns>
        ///// <remarks>
        /////     <para>
        /////         The inherited class can override this method to visit children nodes of this expression.
        /////     </para>
        ///// </remarks>
        //protected abstract Expression[] VisitOtherChildren(ExpressionVisitor visitor);

        ///// <summary>
        /////     <para>
        /////         Updates the expression with the specified query expression and updated children.
        /////     </para>
        ///// </summary>
        ///// <param name="queryExpression">The query expression.</param>
        ///// <param name="updatedChildren">The updated children.</param>
        ///// <returns>A new <see cref="ChainedQueryExpression"/> instance if the query expression or children have changed; otherwise, the current instance.</returns>
        //protected virtual ChainedQueryExpression Update(Expression queryExpression, Expression[] updatedChildren)
        //{
        //    if (queryExpression == Query && this.NoChildrenChanged(updatedChildren))
        //    {
        //        return this;
        //    }
        //    return this.CreateUpdatedExpression(queryExpression, updatedChildren);
        //}

        ///// <inheritdoc />
        //protected override Expression VisitChildren(ExpressionVisitor visitor)
        //{
        //    var queryExpression = visitor.Visit(Query);
        //    Expression[] updatedChildren = this.VisitOtherChildren(visitor);
        //    return Update(queryExpression, updatedChildren);
        //}
    }
}
