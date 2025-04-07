using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionExtensions
{
    // x.NavProp1().NavProp2().NavProp3().Column
    //  MemberExpression:
    //      Member: Column
    //      Expression: InvokeExpression
    //          Expression: MemberExpression
    //              Member: NavProp3
    //              Expression: InvokeExpression
    //                  Expression: MemberExpression
    //                      Member: NavProp2
    //                      Expression: InvokeExpression
    //                          Expression: MemberExpression
    //                              Member: NavProp1
    //                              Expression: ParameterExpression

    // MemberExpression:
    //      Member: Column
    //      Expression: NavigationExpression
    //          NavigationProperty: NavProp3
    //          OtherDataSource: SubEntity3
    //          JoinExpression: otherDataSource3 => otherDataSource2.PK == otherDataSource3.FK
    //          JoinType: Inner
    //          SourceExpression: NavigationExpression
    //              NavigationProperty: NavProp2
    //              OtherDataSource: SubEntity2
    //              JoinExpression: otherDataSource2 => otherDataSource1.PK == otherDataSource2.FK
    //              JoinType: Inner
    //              SourceExpression: NavigationExpression
    //                  NavigationProperty: NavProp1
    //                  OtherDataSource: SubEntity1
    //                  JoinExpression: otherDataSource1 => x.PK == otherDataSource1.FK
    //                  JoinType: Inner
    //                  SourceExpression: ParameterExpression

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
    public class NavigationExpression : Expression
    {
        /// <inheritdoc />
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigationExpression"/> class.
        ///     </para>
        ///     <para>
        ///         This constructor sets up the navigation expression with the specified parameters.
        ///     </para>
        /// </summary>
        /// <param name="sourceExpression">The source expression.</param>
        /// <param name="navigationProperty">The navigation property name.</param>
        /// <param name="joinedDataSource">The joined data source expression.</param>
        /// <param name="joinedDataSourceType">The type of the joined data source the typeof(T) from IEnumerable&lt;T&gt;.</param>
        /// <param name="joinCondition">The join condition expression.</param>
        /// <param name="joinType">The type of join to be used.</param>
        public NavigationExpression(Expression sourceExpression, string navigationProperty, Expression joinedDataSource, Type joinedDataSourceType, LambdaExpression joinCondition, SqlJoinType joinType)
        {
            this.SourceExpression = sourceExpression;
            this.NavigationProperty = navigationProperty;
            this.JoinedDataSource = joinedDataSource;
            this.JoinCondition = joinCondition;
            this.SqlJoinType = joinType;
            this.Type = joinedDataSourceType;
        }

        /// <summary>
        ///     <para>
        ///         Gets the source expression.
        ///     </para>
        /// </summary>
        public Expression SourceExpression { get; }

        /// <summary>
        ///     <para>
        ///         Gets the navigation property name.
        ///     </para>
        /// </summary>
        public string NavigationProperty { get; }

        /// <summary>
        ///     <para>
        ///         Gets the joined data source expression.
        ///     </para>
        /// </summary>
        public Expression JoinedDataSource { get; }

        /// <summary>
        ///     <para>
        ///         Gets the join condition expression.
        ///     </para>
        /// </summary>
        public LambdaExpression JoinCondition { get; }

        /// <summary>
        ///     <para>
        ///         Gets the type of join to be used.
        ///     </para>
        /// </summary>
        public SqlJoinType SqlJoinType { get; }

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
        /// <param name="navigationProperty">The new navigation property name.</param>
        /// <param name="joinedDataSource">The new joined data source expression.</param>
        /// <param name="joinCondition">The new join condition expression.</param>
        /// <param name="joinType">The new type of join to be used.</param>
        /// <returns>A new <see cref="NavigationExpression"/> instance with the updated values, or the current instance if the values are the same.</returns>
        public virtual NavigationExpression Update(Expression sourceExpression, string navigationProperty, Expression joinedDataSource, LambdaExpression joinCondition, SqlJoinType joinType)
        {
            if (sourceExpression == this.SourceExpression && navigationProperty == this.NavigationProperty && joinedDataSource == this.JoinedDataSource && joinCondition == this.JoinCondition && joinType == this.SqlJoinType)
                return this;
            return new NavigationExpression(sourceExpression, navigationProperty, joinedDataSource, this.Type, joinCondition, joinType);
        }

        /// <summary>
        ///     <para>
        ///         Visits the children of the <see cref="NavigationExpression"/>.
        ///     </para>
        ///     <para>
        ///         This method is used to traverse and potentially modify the expression tree.
        ///     </para>
        /// </summary>
        /// <param name="visitor">The expression visitor.</param>
        /// <returns>The modified expression, if any of the children were modified; otherwise, returns the original expression.</returns>
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var sourceExpression = visitor.Visit(this.SourceExpression);
            var joinedDataSource = visitor.Visit(this.JoinedDataSource);
            if (joinedDataSource?.Type != this.JoinedDataSource?.Type)
                throw new InvalidOperationException("JoinedDataSource type cannot be changed");
            var joinCondition = visitor.VisitAndConvert(this.JoinCondition, $"{nameof(NavigationExpression)}.{nameof(VisitChildren)}");
            return this.Update(sourceExpression, this.NavigationProperty, joinedDataSource, joinCondition, this.SqlJoinType);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the <see cref="NavigationExpression"/>.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the <see cref="NavigationExpression"/>.</returns>
        public override string ToString()
        {
            return $"{this.SourceExpression}->{this.NavigationProperty}";
        }
    }
}
