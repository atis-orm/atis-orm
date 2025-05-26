using Atis.Expressions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Abstract base class for preprocessing navigation expressions that navigate to collections.
    ///     </para>
    /// </summary>
    public abstract class NavigateToManyPreprocessorBase : ExpressionVisitor, IExpressionPreprocessor
    {
        /// <summary>
        ///     <para>
        ///         Creates a queryable collection of the specified type.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of the elements in the queryable collection.</typeparam>
        /// <returns>An <see cref="IQueryable{T}"/> representing the queryable collection.</returns>
        protected abstract IQueryable<T> CreateQuery<T>();

        /// <summary>
        ///     <para>
        ///         Gets the entity type associated with the specified expression node.
        ///     </para>
        /// </summary>
        /// <param name="node">The expression node.</param>
        /// <returns>The <see cref="Type"/> of the entity.</returns>
        protected abstract Type GetEntityType(Expression node);

        /// <summary>
        ///     <para>
        ///         Determines whether the specified expression node represents a navigation expression.
        ///     </para>
        /// </summary>
        /// <param name="node">The expression node.</param>
        /// <returns><c>true</c> if the node is a navigation expression; otherwise, <c>false</c>.</returns>
        protected abstract bool IsNavigationExpression(Expression node);

        /// <summary>
        ///     <para>
        ///         Gets the navigation information associated with the specified expression node.
        ///     </para>
        /// </summary>
        /// <param name="node">The expression node.</param>
        /// <returns>A <see cref="NavigationInfo"/> object containing the navigation information.</returns>
        protected abstract NavigationInfo GetNavigationInfo(Expression node);

        /// <inheritdoc/>
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);
        }

        /// <inheritdoc/>
        public override Expression Visit(Expression node)
        {
            if (node is null) return null;

            var updatedNode = base.Visit(node);

            if (this.IsNavigationExpression(updatedNode))
            {
                // node would be something like this
                //      x.NavLines
                var navigationInfo = this.GetNavigationInfo(updatedNode);
                if (navigationInfo.NavigationType == NavigationType.ToChildren)
                {
                    var parentExpression = this.GetParentExpression(updatedNode);      // this should return "x"
                    var queryExpression = this.GetQueryExpression(navigationInfo, updatedNode, parentExpression);
                    return queryExpression;
                }
            }

            return updatedNode;
        }

        /// <summary>
        ///     <para>
        ///         Gets the parent expression of the specified navigation expression node.
        ///     </para>
        /// </summary>
        /// <param name="node">The navigation expression node.</param>
        /// <returns>The parent expression.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the node is not a <see cref="MemberExpression"/>.</exception>
        protected virtual Expression GetParentExpression(Expression node)
        {
            if (node is MemberExpression memberExpression)
                return memberExpression.Expression;

            throw new InvalidOperationException($"node is not a MemberExpression");
        }

        /// <summary>
        /// Gets the name of the navigation property represented by the specified expression node.
        /// </summary>
        /// <param name="node">The expression node.</param>
        /// <returns>The name of the navigation property.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the node is not a <see cref="MemberExpression"/> or if the member name is null.</exception>
        protected virtual string GetNavigationName(Expression node)
        {
            if (node is MemberExpression memberExpression)
                return memberExpression.Member?.Name ??
                        throw new InvalidOperationException("MemberExpression.Member is null (which is not an expected behavior)");

            throw new InvalidOperationException($"node is not a MemberExpression");
        }

        /// <summary>
        ///     <para>
        ///         Gets the query expression for the specified navigation information, navigation node, and parent expression.
        ///     </para>
        /// </summary>
        /// <param name="navigationInfo">The navigation information.</param>
        /// <param name="navigationNode">The navigation expression node.</param>
        /// <param name="parentExpression">The parent expression.</param>
        /// <returns>The query expression.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <c>CreateQueryInternal</c> method is not found or returns null.</exception>
        protected virtual Expression GetQueryExpression(NavigationInfo navigationInfo, Expression navigationNode, Expression parentExpression)
        {
            var navigationName = this.GetNavigationName(navigationNode);
            var entityType = this.GetEntityType(navigationNode);
            // CreateNavSubQueryFuncCall<>
            var createNavSubQueryFuncCallOpen = typeof(NavigateToManyPreprocessorBase).GetMethod(nameof(GetQueryExpressionInternal), BindingFlags.NonPublic | BindingFlags.Instance)
                                                ??
                                                throw new InvalidOperationException("GetQueryExpression() method not found");
            // CreateNavSubQueryFuncCall<entityType>
            var createNavSubQueryFuncCall = createNavSubQueryFuncCallOpen.MakeGenericMethod(entityType);
            var result = createNavSubQueryFuncCall.Invoke(this, new object[] { navigationInfo, parentExpression }) as Expression
                                ??
                                throw new InvalidOperationException("GetQueryExpression() returned null");

            var subQueryNavigation = new SubQueryNavigationExpression(result, navigationName);

            return subQueryNavigation;
        }

        private Expression GetQueryExpressionInternal<T>(NavigationInfo navigationInfo, Expression parentExpression)
        {
            if (navigationInfo.JoinedSource != null)
            {
                var joinedSourceLambda = navigationInfo.JoinedSource;
                var joinedSourceBody = joinedSourceLambda.Body;
                // below will be like this new Queryable<ChildTable>()
                joinedSourceBody = ExpressionReplacementVisitor.Replace(joinedSourceLambda.Parameters[0], parentExpression, joinedSourceBody);
                var predicate = this.CreatePredicate<T>(navigationInfo, parentExpression);
                joinedSourceBody = Expression.Call(typeof(Queryable), nameof(Queryable.Where), new[] { typeof(T) }, joinedSourceBody, predicate);
                return joinedSourceBody;
            }
            else
            {
                return this.CreateQueryInternal<T>(navigationInfo, parentExpression).Expression;
            }
        }

        private IQueryable<T> CreateQueryInternal<T>(NavigationInfo navigationInfo, Expression parentExpression)
        {
            var predicate = this.CreatePredicate<T>(navigationInfo, parentExpression);
            var query = this.CreateQuery<T>();
            query = query.Where(predicate);
            return query;
        }

        private Expression<Func<T, bool>> CreatePredicate<T>(NavigationInfo navigationInfo, Expression parentExpression)
        {
            var relationLambda = navigationInfo.JoinCondition;

            /* 
             * relationLambda will be like this
             * 
             * parent, child => parent.PK == child.FK       <- this is actual join expression
             *    \         \________
             *     \                 \
             * Parameter 0           Parameter 1
             * 
             * Example:
             *      x.NavLines.Any(y => y.Field > 5)
             * should be converted to
             *      new Queryable<Line>().Where(     child => x.PK == child.FK  ).Any(y => y.Field > 5)
             *                         _________/       \      \________
             *                        /              Parameter 1        |
             *                       |                                  |
             *                  Parameter 0 is replaced with parentExpression
             *                                   
             * */

            var parameterToReplace = relationLambda.Parameters[0];
            var parameterToKeep = relationLambda.Parameters[1];

            var predicateBody = ExpressionReplacementVisitor.Replace(parameterToReplace, parentExpression, relationLambda.Body);
            var predicateLambda = Expression.Lambda<Func<T, bool>>(predicateBody, parameterToKeep);
            return predicateLambda;
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {
            // do nothing
        }
    }
}
