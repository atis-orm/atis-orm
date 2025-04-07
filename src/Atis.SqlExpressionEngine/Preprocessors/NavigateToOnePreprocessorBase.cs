using Atis.Expressions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Base class for preprocessing navigation expressions that navigate to a single entity.
    ///     </para>
    /// </summary>
    public abstract class NavigateToOnePreprocessorBase : ExpressionVisitor, IExpressionPreprocessor
    {

        private readonly static MethodInfo createJoinedDataSourceOpenMethodInfo = typeof(NavigateToOnePreprocessorBase).GetMethod(nameof(CreateJoinedDataSourceGen), BindingFlags.NonPublic | BindingFlags.Instance);
        
        private readonly IReflectionService reflectionService;
        private readonly Stack<Expression> expressionsStack = new Stack<Expression>();

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigateToOnePreprocessorBase"/> class.
        ///     </para>
        /// </summary>
        /// <param name="reflectionService">The reflection service used for type and property information.</param>
        public NavigateToOnePreprocessorBase(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }


        /// <inheritdoc />
        public void Initialize()
        {

        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);   
        }

        /// <inheritdoc />
        public override Expression Visit(Expression node)
        {
            if (node is null) return null;

            this.expressionsStack.Push(node);

            var updatedNode = base.Visit(node);

            var stack = this.expressionsStack.ToArray();
            try
            {
                if (this.IsNavigation(updatedNode, stack))
                {
                    // IsNavigation should return true in-case if MemberExpression itself is a navigation
                    // e.g. NavigationProp().Column             <- should return false
                    //      x.NavigationProp()                  <- should return true
                    //      x.NavigationProp                    <- should return true
                    //      ParentNavigation().NavigationProp() <- should return true
                    //      ParentNavigation.NavigationProp     <- should return true

                    // this method will be called on the leaf-node which means in the following case
                    //      x.NavProp1().NavProp2().NavProp3().Column
                    // we'll land here for x.NavProp1()

                    var navigationInfo = this.GetNavigationInfo(updatedNode, stack);

                    // this should return the parent expression, for example, in-case of
                    //          x.NavProp1()                            -> should return x
                    //          ParentNavigation().NavigationProp()     -> should return ParentNavigation()
                    //          x.NavProp1                              -> should return x
                    //          ParentNavigation.NavigationProp         -> should return ParentNavigation

                    // however, since this method is being executed from leaf to root, so we'll be converting
                    // each expression and we'll be getting the NavigationExpression as parent in later cases,
                    // for example, x.NavProp1() is converted to a NavigationExpression, so when we'll reach
                    // to node = NavProp1().NavProp2(), it will be received here as node = ->NavProp1->NavProp2()

                    Expression sourceExpression = this.GetParentExpression(updatedNode, stack);
                    string navigationProperty = this.GetNavigationPropertyName(updatedNode, stack);
                    Expression joinedDataSource = navigationInfo.JoinedSource ?? this.CreateJoinedDataSource(updatedNode.Type);
                    var joinedDataSourceType = this.GetJoinedDataSourceType(joinedDataSource);
                    LambdaExpression joinCondition = null;
                    if (navigationInfo.JoinCondition != null)
                        joinCondition = this.CreateJoinCondition(sourceExpression, navigationInfo.JoinCondition, navigationInfo.NavigationType);
                    SqlJoinType joinType = this.GetJoinType(navigationInfo);
                    if (joinType == SqlJoinType.Inner)                                      // if new join is inner
                    {
                        if (sourceExpression is NavigationExpression parentNavigation)      // if parent is also a navigation
                        {
                            if (parentNavigation.SqlJoinType == SqlJoinType.Left || parentNavigation.SqlJoinType == SqlJoinType.OuterApply)
                            {
                                joinType = SqlJoinType.Left;
                            }
                        }
                    }
                    var navigationExpression = new NavigationExpression(sourceExpression, navigationProperty, joinedDataSource, joinedDataSourceType, joinCondition, joinType);
                    return navigationExpression;
                }
                return updatedNode;
            }
            finally
            {
                this.expressionsStack.Pop();
            }
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the joined data source.
        ///     </para>
        /// </summary>
        /// <param name="joinedDataSource">The joined data source expression.</param>
        /// <returns>The type of the joined data source.</returns>
        protected virtual Type GetJoinedDataSourceType(Expression joinedDataSource)
        {
            return this.reflectionService.GetEntityTypeFromQueryableType(joinedDataSource.Type);
        }

        /// <summary>
        ///     <para>
        ///         Creates the join condition for the navigation.
        ///     </para>
        /// </summary>
        /// <param name="sourceExpression">The source expression.</param>
        /// <param name="joinCondition">The join condition expression.</param>
        /// <param name="navigationType">The type of navigation.</param>
        /// <returns>A lambda expression representing the join condition.</returns>
        protected virtual LambdaExpression CreateJoinCondition(Expression sourceExpression, LambdaExpression joinCondition, NavigationType navigationType)
        {
            // joinCondition will be like this
            //   (parent, child) => parent.PK == child.FK
            // if NavigationType = ToChild
            //      then it means it's 1 to 1 relation and sourceExpression = parent
            //     e.g. itemBase => itemBase.NavItemExtension().Category
            //              ItemBase = parent, NavItemExtension = Navigation from ItemBase to ItemExtension (child)
            //              NavItemExtension.JoinCondition will be (itemBase, itemExtension) => itemBase.ItemId == itemExtension.ItemId
            //
            //          We'll replace `parent` (itemBase) with sourceExpression and remove `parent` from Lambda
            //
            // if NavigationType = ToParent
            //      then it means it's many to 1 relation and sourceExpression = child
            //     e.g. invoiceDetail => invoiceDetail.NavInvoice().InvoiceNum
            //              InvoiceDetail = child, NavInvoice = Navigation from InvoiceDetail to Invoice (parent)
            //              NavInvoice.JoinCondition will be (invoice, invoiceDetail) => invoice.InvoiceId == invoiceDetail.InvoiceId
            //
            //          We'll replace `child` (invoiceDetail) with sourceExpression and remove `child` from Lambda

            if (joinCondition.Parameters.Count != 2)
                throw new NotSupportedException("Join condition should have exactly 2 parameters.");

            ParameterExpression parameterToReplace;
            ParameterExpression parameterToUse;
            if (navigationType == NavigationType.ToSingleChild)
            {
                parameterToReplace = joinCondition.Parameters[0];
                parameterToUse = joinCondition.Parameters[1];
            }
            else if (navigationType == NavigationType.ToParent || navigationType == NavigationType.ToParentOptional)
            {
                parameterToReplace = joinCondition.Parameters[1];
                parameterToUse = joinCondition.Parameters[0];
            }
            else
                throw new InvalidOperationException($"Navigation type '{navigationType}' is not supported.");

            var newJoinConditionBody = ExpressionReplacementVisitor.Replace(parameterToReplace, sourceExpression, joinCondition.Body);
            return Expression.Lambda(newJoinConditionBody, parameterToUse);
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of join to be used for the navigation.
        ///     </para>
        /// </summary>
        /// <param name="navigationInfo">The navigation information.</param>
        /// <returns>The type of join to be used.</returns>
        protected virtual SqlJoinType GetJoinType(NavigationInfo navigationInfo)
        {

            SqlJoinType joinType;
            switch (navigationInfo.NavigationType)
            {
                case NavigationType.ToParent:
                    joinType = SqlJoinType.Inner;
                    break;
                case NavigationType.ToParentOptional:
                    joinType = SqlJoinType.Left;
                    break;
                case NavigationType.ToSingleChild:
                    joinType = SqlJoinType.Left;
                    break;
                default:
                    throw new NotSupportedException($"Navigation type '{navigationInfo.NavigationType}' is not supported.");
            }
            if (navigationInfo.JoinCondition is null)
                joinType = SqlJoinType.OuterApply;
            return joinType;
        }

        /// <summary>
        ///     <para>
        ///         Gets the name of the navigation property.    
        ///     </para>
        /// </summary>
        /// <param name="currentNode">The current expression node.</param>
        /// <param name="expressionStack">The stack of parent expressions.</param>
        /// <returns>The name of the navigation property.</returns>
        protected abstract string GetNavigationPropertyName(Expression currentNode, Expression[] expressionStack);

        /// <summary>
        ///     <para>
        ///         Gets the parent expression of the current node.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, if currentNode is <c>x.NavProp</c>, then the parent expression is <c>x</c>.
        ///     </para>
        /// </remarks>
        /// <param name="currentNode">The current expression node.</param>
        /// <param name="expressionStack">The stack of parent expressions.</param>
        /// <returns>The parent expression.</returns>
        protected abstract Expression GetParentExpression(Expression currentNode, Expression[] expressionStack);

        /// <summary>
        ///     <para>
        ///         Gets the navigation information for the current node.
        ///     </para>
        /// </summary>
        /// <param name="currentNode">The current expression node.</param>
        /// <param name="expressionStack">The stack of parent expressions.</param>
        /// <returns>The navigation information.</returns>
        protected abstract NavigationInfo GetNavigationInfo(Expression currentNode, Expression[] expressionStack);

        /// <summary>
        ///     <para>
        ///         Determines whether the current node represents a navigation.
        ///     </para>
        /// </summary>
        /// <param name="currentNode">The current expression node.</param>
        /// <param name="expressionStack">The stack of parent expressions.</param>
        /// <returns><c>true</c> if the current node represents a navigation; otherwise, <c>false</c>.</returns>
        protected abstract bool IsNavigation(Expression currentNode, Expression[] expressionStack);

        /// <summary>
        ///     <para>
        ///         Gets the query provider for the current context.
        ///     </para>
        /// </summary>
        /// <returns>The query provider.</returns>
        protected abstract IQueryProvider GetQueryProvider();

        /// <summary>
        ///     <para>
        ///         Creates a joined data source expression for the specified parent type.
        ///     </para>
        /// </summary>
        /// <param name="parentType">The type of the parent entity.</param>
        /// <returns>The joined data source expression.</returns>
        protected virtual Expression CreateJoinedDataSource(Type parentType)
        {
            // here we will create the CreateJoinedDataSource<TParent>()
            var closedMethodInfo = createJoinedDataSourceOpenMethodInfo.MakeGenericMethod(parentType);
            // now we will call the method to get CreateJoinedDataSource
            var dataSourceExpression = (Expression)closedMethodInfo.Invoke(this, Array.Empty<object>());
            return dataSourceExpression;
        }

        /// <summary>
        /// Creates a joined data source expression for the specified parent type.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent entity.</typeparam>
        /// <returns>The joined data source expression.</returns>
        private Expression CreateJoinedDataSourceGen<TParent>()
        {
            var q = this.GetQueryProvider().DataSet<TParent>();
            return q.Expression;
        }
    }
}
