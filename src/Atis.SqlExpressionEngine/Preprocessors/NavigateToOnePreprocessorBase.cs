using Atis.SqlExpressionEngine.ExpressionExtensions;
using System.Linq.Expressions;
using Atis.Expressions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Base class for preprocessing navigation expressions that navigate to a single entity.
    ///     </para>
    /// </summary>
    public abstract class NavigateToOnePreprocessorBase : ExpressionVisitor, IExpressionPreprocessor
    {
        private class NavigationJoinMetadata
        {
            public NavigationJoinMetadata(Expression queryMethod)
            {
                this.QueryMethod = queryMethod ?? throw new ArgumentNullException(nameof(queryMethod));
                this.Navigations = new Dictionary<string, (LambdaExpression ParentExpression, NavigationInfo NavInfo)>();
            }
            public Expression QueryMethod { get; }
            public Dictionary<string, (LambdaExpression ParentExpression, NavigationInfo NavInfo)> Navigations { get; }
        }

        private readonly Stack<Expression> stack = new Stack<Expression>();
        private readonly Stack<Expression> queryMethodStack = new Stack<Expression>();
        private readonly Dictionary<Expression, NavigationJoinMetadata> navigationJoinMetadata = new Dictionary<Expression, NavigationJoinMetadata>();

        /// <inheritdoc />
        public void Initialize()
        {
            this.stack.Clear();
            this.queryMethodStack.Clear();
            this.navigationJoinMetadata.Clear();
            this.newNodeVsOldNode.Clear();
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression expression)
        {
            this.Initialize();
            this.flowType = FlowType.Extract;
            var visited = this.Visit(expression);
            this.flowType =  FlowType.Inject;
            visited = this.Visit(visited);
            return visited;
        }

        private enum FlowType
        {
            Extract,
            Inject,
        }
        private FlowType? flowType;

        /// <inheritdoc />
        public override Expression Visit(Expression node)
        {
            if (node is null) return null;

            var wasQueryMethod = false;
            this.stack.Push(node);
            if (this.IsQueryMethod(node))
            {
                wasQueryMethod = true;
                this.queryMethodStack.Push(node);
            }

            var visited = base.Visit(node);

            if (flowType == FlowType.Extract)
            {
                var stackArray = stack.ToArray();
                if (this.TryGetNavigationInfo(visited, stackArray, out Expression parentExpression, out NavigationInfo navigationInfo))
                {
                    var joinedDataSourceType = node.Type;
                    this.CreateMetadataForNavigationJoin(parentExpression, navigationInfo);
                    visited = new NavigationMemberExpression(parentExpression, navigationInfo.PropertyName, joinedDataSourceType);
                }
            }
            else if (flowType ==  FlowType.Inject)
            {
                if (this.newNodeVsOldNode.TryGetValue(node, out var oldNode))
                {
                    if (this.navigationJoinMetadata.TryGetValue(oldNode, out NavigationJoinMetadata navigationJoinMetadata))
                    {
                        visited = this.WrapFirstArgInNavigationJoinCall(navigationJoinMetadata, visited);
                        // since we are exiting the query method so we no longer need to keep
                        // the method's meta data in the dictionary
                        this.navigationJoinMetadata.Remove(oldNode);
                    }
                }
            }
            this.stack.Pop();
            if (wasQueryMethod)
            {
                this.queryMethodStack.Pop();
            }

            if (flowType == FlowType.Extract)
            {
                // map node with visited
                newNodeVsOldNode[visited] = node;
            }

            return visited;
        }

        private readonly Dictionary<Expression, Expression> newNodeVsOldNode = new Dictionary<Expression, Expression>();

        protected abstract bool IsQueryMethod(Expression node);
        protected abstract bool TryGetNavigationInfo(Expression node, IReadOnlyCollection<Expression> stackArray, out Expression parentExpression, out NavigationInfo navigationInfo);
        protected abstract bool DoesParameterBelongToQueryMethod(ParameterExpression parameter, Expression queryMethod);
        protected abstract Expression GetQuerySourceArgumentFromQueryMethod(Expression queryMethod);
        protected abstract Expression CreateQueryMethodCall(Expression oldQueryMethodNode, Expression wrappedQuerySourceArg);
        protected abstract IQueryProvider GetQueryProvider();
        protected abstract Type GetEnumerableEntityType(Type enumerableType);
        protected abstract bool TryExtractQueryMethodInjectionPoint(ParameterExpression parameterExpression, Expression parentExpression, NavigationInfo navigationInfo, out LambdaExpression parentExpressionLambda, out Expression queryMethod);



        private Expression WrapFirstArgInNavigationJoinCall(NavigationJoinMetadata navigationMetadata, Expression queryMethodNode)
        {
            var querySourceArgument = this.GetQuerySourceArgumentFromQueryMethod(queryMethodNode)
                                        ??
                                        throw new InvalidOperationException($"Query Source Argument was not extracted from queryMethodNode '{queryMethodNode}'");
            var querySourceEntityType = this.GetEnumerableEntityType(querySourceArgument.Type)
                                            ??
                                            throw new InvalidOperationException($"Query Source Entity Type was not extracted from querySourceArgument '{querySourceArgument}'");
            var wrappedQuerySource = querySourceArgument;
            var i = 0;
            var navigations = navigationMetadata.Navigations.Values.ToArray();
            foreach (var navigation in navigations)
            {
                var navigationProperty = navigation.NavInfo.PropertyName;
                var joinedDataSource = navigation.NavInfo.JoinedSource
                                        ??
                                        throw new InvalidOperationException($"JoinedSource property is null");
                var joinCondition = navigation.NavInfo.JoinCondition;
                var sqlJoinType = this.GetJoinType(navigation.NavInfo);
                if (i > 0 && sqlJoinType == SqlJoinType.Inner)                                      // if new join is inner
                {
                    var parentNavigation = navigations[i - 1];
                    var parentNavigationSqlJoinType = this.GetJoinType(parentNavigation.NavInfo);
                    if (parentNavigationSqlJoinType == SqlJoinType.Left || parentNavigationSqlJoinType == SqlJoinType.OuterApply)
                    {
                        sqlJoinType = SqlJoinType.Left;
                    }
                }
                wrappedQuerySource = new NavigationJoinCallExpression(wrappedQuerySource, navigation.ParentExpression, navigationProperty, joinedDataSource, joinCondition, sqlJoinType, navigation.NavInfo.NavigationType);
                i++;
            }
            var updatedQueryMethodCall = this.CreateQueryMethodCall(queryMethodNode, wrappedQuerySource);
            return updatedQueryMethodCall;
        }

        private SqlJoinType GetJoinType(NavigationInfo navigationInfo)
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

        private void CreateMetadataForNavigationJoin(Expression parentExpression, NavigationInfo navigationInfo)
        {
            ParameterExpression parameterExpression = this.ExtractParameter(parentExpression);
            
            if (parameterExpression is null)
                throw new InvalidOperationException($"{nameof(parameterExpression)} is null");      // should never happen

            if (this.TryExtractQueryMethodInjectionPoint(parameterExpression, parentExpression, navigationInfo, out LambdaExpression parentExpressionLambda, out Expression queryMethod))
            {
                var parentExpressionBody = parentExpressionLambda.Body;
                var navigationKey = $"{parentExpressionBody}.{navigationInfo.PropertyName}";
                if (!this.navigationJoinMetadata.TryGetValue(queryMethod, out var navigationJoinMetadata))
                {
                    navigationJoinMetadata = new NavigationJoinMetadata(queryMethod);
                    this.navigationJoinMetadata.Add(queryMethod, navigationJoinMetadata);
                }
                if (navigationJoinMetadata.Navigations.Count == 0 || !navigationJoinMetadata.Navigations.ContainsKey(navigationKey))
                {
                    navigationJoinMetadata.Navigations.Add(navigationKey, (parentExpressionLambda, navigationInfo));
                }
            }
        }

        protected virtual bool TryGetQueryMethodFromParameter(ParameterExpression parameterExpression, out Expression queryMethod)
        {
            if (this.queryMethodStack.Count > 0)
            {
                foreach (var method in this.queryMethodStack)
                {
                    if (this.DoesParameterBelongToQueryMethod(parameterExpression, method))
                    {
                        queryMethod = method;
                        return true;
                    }
                }
            }
            queryMethod = null;
            return false;
        }

        protected virtual ParameterExpression ExtractParameter(Expression expression)
        {
            if (expression is NavigationMemberExpression navMember)
                return this.ExtractParameter(navMember.Expression);
            else if (expression is MemberExpression memberExpression)
                return this.ExtractParameter(memberExpression.Expression);
            else if (expression is ParameterExpression parameterExpression)
                return parameterExpression;
            throw new InvalidOperationException($"{nameof(expression)} does not contain {nameof(ParameterExpression)}");
        }
    }
}
