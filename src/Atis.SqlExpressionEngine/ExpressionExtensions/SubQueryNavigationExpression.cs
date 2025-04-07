using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionExtensions
{
    /// <summary>
    ///     <para>
    ///         Represents a sub-query navigation expression.
    ///     </para>
    ///     <para>
    ///         Caution: this is internal class and is not intended to be used by the
    ///         end user and is subject to change without notice.
    ///     </para>
    /// </summary>
    public class SubQueryNavigationExpression : ChainedQueryExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SubQueryNavigationExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="queryExpression">The query expression to be chained.</param>
        /// <param name="navigationProperty">The navigation property.</param>
        public SubQueryNavigationExpression(Expression queryExpression, string navigationProperty)
            : base(queryExpression)
        {
            NavigationProperty = navigationProperty;
        }

        /// <summary>
        ///     <para>
        ///         Gets the navigation property.
        ///     </para>
        /// </summary>
        public string NavigationProperty { get; }

        ///// <inheritdoc />
        //protected override ChainedQueryExpression CreateUpdatedExpression(Expression queryExpression, Expression[] updatedChildren)
        //{
        //    return new SubQueryNavigationExpression(queryExpression, this.NavigationProperty);
        //}

        ///// <inheritdoc />
        //protected override bool NoChildrenChanged(Expression[] updatedChildren)
        //{
        //    // since this class does not have any other children, we can return true
        //    return true;
        //}

        ///// <inheritdoc />
        //protected override Expression[] VisitOtherChildren(ExpressionVisitor visitor)
        //{
        //    // since this class does not have any other children, we can return an empty array
        //    return Array.Empty<Expression>();
        //}

        public SubQueryNavigationExpression Update(Expression query, string navigationProperty)
        {
            if (this.Query == query && this.NavigationProperty == navigationProperty)
                return this;
            return new SubQueryNavigationExpression(query, navigationProperty);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            // this node does not have any children to visit
            var query = visitor.Visit(this.Query);
            return this.Update(query, this.NavigationProperty);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SubQuery({this.Query})";
        }
    }
}
