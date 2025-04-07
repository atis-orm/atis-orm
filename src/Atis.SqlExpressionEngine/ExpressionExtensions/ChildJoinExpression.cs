using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionExtensions
{
    /// <summary>
    ///     <para>
    ///         Represents a join expression between a parent and a child source.
    ///     </para>
    ///     <para>
    ///         This class is used to create a join condition and specify the navigation type and name.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Caution: this is internal class and is not intended to be used by the
    ///         end user and is subject to change without notice.
    ///     </para>
    /// </remarks>
    public class ChildJoinExpression : ChainedQueryExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ChildJoinExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Throws <see cref="ArgumentNullException"/> if parent or childSource is null.
        ///     </para>
        /// </summary>
        /// <param name="parent">The parent expression.</param>
        /// <param name="childSource">The child source expression.</param>
        /// <param name="joinCondition">The join condition as a lambda expression.</param>
        /// <param name="navigationType">The type of navigation.</param>
        /// <param name="navigationName">The name of the navigation.</param>
        public ChildJoinExpression(Expression parent, Expression childSource, LambdaExpression joinCondition, NavigationType navigationType, string navigationName)
            : base(childSource)
        {
            this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            //this.ChildSource = childSource ?? throw new ArgumentNullException(nameof(childSource));
            this.JoinCondition = joinCondition;
            this.NavigationType = navigationType;
            this.NavigationName = navigationName;
            //this.Type = this.ChildSource.Type;
        }

        /// <summary>
        ///     <para>
        ///         Gets the parent expression.
        ///     </para>
        /// </summary>
        public Expression Parent { get; }

        ///// <summary>
        /////     <para>
        /////         Gets the child source expression.
        /////     </para>
        ///// </summary>
        //public Expression ChildSource { get; }

        /// <summary>
        ///     <para>
        ///         Gets the join condition as a lambda expression.
        ///     </para>
        /// </summary>
        public LambdaExpression JoinCondition { get; }

        /// <summary>
        ///     <para>
        ///         Gets the type of navigation.
        ///     </para>
        /// </summary>
        public NavigationType NavigationType { get; }

        /// <summary>
        ///     <para>
        ///         Gets the name of the navigation.
        ///     </para>
        /// </summary>
        public string NavigationName { get; }

        ///// <summary>
        /////     <para>
        /////         Gets the type of the expression.
        /////     </para>
        ///// </summary>
        //public override Type Type { get; }

        ///// <summary>
        /////     <para>
        /////         Gets the node type of this expression.
        /////     </para>
        ///// </summary>
        //public override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        ///     <para>
        ///         Visits the children of the <see cref="ChildJoinExpression"/>.
        ///     </para>
        /// </summary>
        /// <param name="visitor">The expression visitor.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var parent = visitor.Visit(this.Parent);
            var childSource = visitor.Visit(this.Query);
            var joinCondition = visitor.VisitAndConvert(this.JoinCondition, nameof(VisitChildren));
            return Update(parent, childSource, joinCondition, this.NavigationType, this.NavigationName);
        }

        /// <summary>
        ///     <para>
        ///         Updates the <see cref="ChildJoinExpression"/> with new values.
        ///     </para>
        /// </summary>
        /// <param name="parent">The new parent expression.</param>
        /// <param name="childSource">The new child source expression.</param>
        /// <param name="joinCondition">The new join condition as a lambda expression.</param>
        /// <param name="navigationType">The new type of navigation.</param>
        /// <param name="navigationName">The new name of the navigation.</param>
        /// <returns>A new <see cref="ChildJoinExpression"/> if any value is different; otherwise, returns the current instance.</returns>
        public ChildJoinExpression Update(Expression parent, Expression childSource, LambdaExpression joinCondition, NavigationType navigationType, string navigationName)
        {
            if (parent == this.Parent && childSource == this.Query && joinCondition == this.JoinCondition && navigationType == this.NavigationType && navigationName == this.NavigationName)
            {
                return this;
            }
            return new ChildJoinExpression(parent, childSource, joinCondition, navigationType, navigationName);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the <see cref="ChildJoinExpression"/>.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the <see cref="ChildJoinExpression"/>.</returns>
        public override string ToString()
        {
            return $"ChildJoin({this.Parent} <1-*> {this.Query} on {this.JoinCondition})";
        }
    }
}
