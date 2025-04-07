using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine
{
    public enum NavigationType
    {
        ToParent,
        ToParentOptional,
        ToChildren,
        ToSingleChild,
    }
    public class NavigationInfo
    {
        public NavigationType NavigationType { get; }
        /// <summary>
        ///     <para>
        ///         Gets or sets the Join Condition for the navigation property.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This property must be set like this,
        ///     </para>
        ///     <code>
        ///         (parentEntity, childEntity) => parentEntity.PK == childEntity.FK
        ///     </code>
        /// </remarks>
        public LambdaExpression JoinCondition { get; }
        public Expression JoinedSource { get; }
        public NavigationInfo(NavigationType navigationType, LambdaExpression joinCondition, Expression joinedSource)
        {
            NavigationType = navigationType;
            this.JoinCondition = joinCondition;
            this.JoinedSource = joinedSource;
        }
    }
}