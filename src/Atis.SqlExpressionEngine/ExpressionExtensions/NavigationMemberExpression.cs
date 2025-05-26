using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionExtensions
{
    /// <summary>
    ///     <para>
    ///         Represents a navigation expression in a LINQ to SQL query.
    ///     </para>
    ///     <para>
    ///         This class is used to handle navigation properties in a query, allowing for complex
    ///         joins and navigation through related entities.
    ///     </para>
    ///     <para>
    ///         Caution: this is internal class and is not intended to be used by the
    ///         end user and is subject to change without notice.
    ///     </para>
    /// </summary>
    public class NavigationMemberExpression : Expression
    {
        /// <inheritdoc />
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigationMemberExpression"/> class.
        ///     </para>
        ///     <para>
        ///         This constructor sets up the navigation expression with the specified parameters.
        ///     </para>
        /// </summary>
        /// <param name="expression">The source expression.</param>
        /// <param name="navigationProperty">The navigation property name.</param>
        /// <param name="navigationType"></param>
        public NavigationMemberExpression(Expression expression, string navigationProperty, Type navigationType)
        {
            this.Expression = expression;
            this.NavigationProperty = navigationProperty;
            this.Type = navigationType;
        }

        /// <summary>
        ///     <para>
        ///         Gets the source expression.
        ///     </para>
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        ///     <para>
        ///         Gets the navigation property name.
        ///     </para>
        /// </summary>
        public string NavigationProperty { get; }

        /// <inheritdoc />
        public override Type Type { get; }

        /// <summary>
        ///     <para>
        ///         Updates the navigation expression with new values.
        ///     </para>
        ///     <para>
        ///         If the new values are the same as the current values, the current instance is returned.
        ///     </para>
        /// </summary>
        /// <param name="sourceExpression">The new source expression.</param>
        /// <returns>A new <see cref="NavigationMemberExpression"/> instance with the updated values, or the current instance if the values are the same.</returns>
        public NavigationMemberExpression Update(Expression sourceExpression)
        {
            if (sourceExpression == this.Expression)
                return this;
            return new NavigationMemberExpression(sourceExpression, this.NavigationProperty, this.Type);
        }

        /// <summary>
        ///     <para>
        ///         Visits the children of the <see cref="NavigationMemberExpression"/>.
        ///     </para>
        ///     <para>
        ///         This method is used to traverse and potentially modify the expression tree.
        ///     </para>
        /// </summary>
        /// <param name="visitor">The expression visitor.</param>
        /// <returns>The modified expression, if any of the children were modified; otherwise, returns the original expression.</returns>
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var sourceExpression = visitor.Visit(this.Expression);
            return this.Update(sourceExpression);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the <see cref="NavigationMemberExpression"/>.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the <see cref="NavigationMemberExpression"/>.</returns>
        public override string ToString()
        {
            return $"{this.Expression}->{this.NavigationProperty}";
        }
    }
}
