using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.SqlExpressionEngine.UnitTest.Preprocessors
{
    public class NavigateToOnePreprocessor : NavigateToOnePreprocessorBase
    {
        private readonly static MethodInfo createJoinedDataSourceOpenMethodInfo = typeof(NavigateToOnePreprocessor).GetMethod(nameof(CreateJoinedDataSourceGen), BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly IReflectionService reflectionService;
        private readonly IQueryProvider queryProvider;

        private enum NavigationPropertyType
        {
            None,
            EntityRelationClass,
            RelationAttribute
        }

        public NavigateToOnePreprocessor(IReflectionService reflectionService, IQueryProvider queryProvider)
        {
            this.reflectionService = reflectionService;
            this.queryProvider = queryProvider;
        }

        protected override bool DoesParameterBelongToQueryMethod(ParameterExpression parameter, Expression queryMethod)
        {
            if (queryMethod is MethodCallExpression methodCall)
            {
                foreach (var arg in methodCall.Arguments)
                {
                    var lambdaExpression = (arg as UnaryExpression)?.Operand as LambdaExpression
                                            ??
                                            (arg as LambdaExpression);
                    if (lambdaExpression?.Parameters.Contains(parameter) ?? false)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override bool TryExtractQueryMethodInjectionPoint(ParameterExpression parameterExpression, Expression parentExpression, NavigationInfo navigationInfo, out LambdaExpression parentExpressionLambda, out Expression queryMethod)
        {
            if (parameterExpression is null)
                throw new ArgumentNullException(nameof(parameterExpression));
            if (parentExpression is null)
                throw new ArgumentNullException(nameof(parentExpression));
            if (navigationInfo is null)
                throw new ArgumentNullException(nameof(navigationInfo));

            if (this.TryGetQueryMethodFromParameter(parameterExpression, out var firstTryQueryMethodCall))
            {
                Expression resultantInjectionPoint = firstTryQueryMethodCall;
                if (firstTryQueryMethodCall is MethodCallExpression aggregateMethodCall && 
                        this.reflectionService.IsAggregateMethod(aggregateMethodCall))
                {
                    var injectionPointExpression = this.GetInjectionPoint(parameterExpression, aggregateMethodCall);
                    if (injectionPointExpression != null)
                    {
                        resultantInjectionPoint = injectionPointExpression;
                    }
                }
                var newParentParam = Expression.Parameter(parameterExpression.Type, "s_p");
                var parentExpressionReplacedParameter = ExpressionReplacementVisitor.Replace(parameterExpression, newParentParam, parentExpression);
                parentExpressionLambda = Expression.Lambda(parentExpressionReplacedParameter, newParentParam);
                queryMethod = resultantInjectionPoint;
                return true;
            }
            parentExpressionLambda = null;
            queryMethod = null;
            return false;
        }

        private Expression GetInjectionPoint(ParameterExpression parameterExpression, MethodCallExpression methodCallExpression)
        {
            // here we are trying to apply the NavJoin on the GroupBy level
            // e.g.  db.Table1.GroupBy(p1 => p1.Field1).Select(p2 => new { p2.Key, Total = Queryable.Sum(p2, p3 => p3.NavTable2().TF) });
            //          Select(GroupBy(db.Table1, p1 => p1.Field1), p2 => new { p2.Key, Total = Queryable.Sum(p2, p3 => p3.NavTable2().TF) });
            // In above example, if we were to inject the NavJoin, we would have injected at p3 level and it would
            // look like this,
            //      db.Table1.GroupBy(p1 => p1.Field1).Select(p2 => new { p2.Key, Total = Queryable.Sum(NavJoin(p2, ...), p3 => p3.NavTable2().TF) });
            // But below will inject the NavJoin on the before GroupBy
            //      GroupBy(NavJoin(dbTable1, ....), p1 => p1.Field1).Select(p2 => new { p2.Key, Total = Queryable.Sum(p2, p3 => p3.NavTable2().TF) });
            //

            // parameterExpression would be `p3`, and methodCallExpression would be `Sum`

            if (methodCallExpression.Arguments.FirstOrDefault() is ParameterExpression parentParam)         // p2
            {
                if (this.TryGetQueryMethodFromParameter(parentParam, out var outerExpression))              // maybe Select above GroupBy
                {
                    if (outerExpression is MethodCallExpression outerMethod                                 // confirmed Select (or any other method)
                        &&
                        outerMethod.Arguments.FirstOrDefault() is MethodCallExpression outerMethodArg0      // maybe GroupBy
                        &&
                        outerMethodArg0 is MethodCallExpression outerMethodArg0AsMethodCall                 // ok it's a MethodCallExpression 
                        &&
                        outerMethodArg0AsMethodCall.Method.Name == nameof(Enumerable.GroupBy)               // confirmed it's GroupBy
                        )
                    {
                        return outerMethodArg0AsMethodCall;//.Arguments[0];                                    // this needs to be wrapped
                    }
                }
            }
            return null;
        }

        protected override bool IsQueryMethod(Expression node)
        {
            return node is MethodCallExpression;
        }

        protected override bool TryGetNavigationInfo(Expression node, IReadOnlyCollection<Expression> stackArray, out Expression parentExpression, out NavigationInfo navigationInfo)
        {
            var memberExpression = this.GetMemberExpression(node, stackArray);
            if (memberExpression != null)
            {
                var member = memberExpression.Member;
                var modelType = memberExpression.Expression?.Type;
                var navigationPropertyType = this.GetNavigationPropertyType(memberExpression.Expression?.Type, memberExpression.Member);
                if (navigationPropertyType != NavigationPropertyType.None)
                {
                    switch (navigationPropertyType)
                    {
                        case NavigationPropertyType.EntityRelationClass:
                            {
                                LambdaExpression relationLambda = null;
                                var (entityRelation, navigationType) = GetRelationAndTypeByEntityRelationClass(modelType, member);
                                // entityRelation.JoinExpression can be null for outer apply
                                if (entityRelation.JoinExpression != null)
                                    relationLambda = entityRelation.JoinExpression as LambdaExpression
                                                            ?? (entityRelation.JoinExpression as UnaryExpression)?.Operand as LambdaExpression
                                                            ?? throw new InvalidOperationException("Invalid relation expression");
                                LambdaExpression otherDataSource;
                                Type joinedSourceParamType;
                                Type joinedSourceType;
                                if (navigationType == NavigationType.ToParent || navigationType == NavigationType.ToParentOptional)
                                {
                                    otherDataSource = entityRelation.FromChildToParent(this.queryProvider);
                                    joinedSourceParamType = entityRelation.ChildType;
                                    joinedSourceType = entityRelation.ParentType;
                                }
                                else
                                {
                                    otherDataSource = entityRelation.FromParentToChild(this.queryProvider);
                                    joinedSourceParamType = entityRelation.ParentType;
                                    joinedSourceType = entityRelation.ChildType;
                                }
                                otherDataSource = otherDataSource ?? this.CreateJoinedDataSource(joinedSourceParamType, joinedSourceType);
                                parentExpression = this.GetParentExpression(node, stackArray);
                                navigationInfo = new NavigationInfo(navigationType, relationLambda, otherDataSource ?? throw new InvalidOperationException("otherDataSource is null"), memberExpression.Member.Name);
                                return true;
                            }
                        case NavigationPropertyType.RelationAttribute:
                            {
                                var (navigationType, relationLambda) = GetNavigationTypeAndRelationLambdaFromRelationAttribute(modelType, member, out var parentEntityType, out var childEntityType);
                                parentExpression = this.GetParentExpression(node, stackArray);
                                Type joinedSourceParamType;
                                Type joinedSourceType;
                                if (navigationType == NavigationType.ToParent || navigationType == NavigationType.ToParentOptional)
                                {
                                    joinedSourceParamType = childEntityType;
                                    joinedSourceType = parentEntityType;
                                }
                                else
                                {
                                    joinedSourceParamType = parentEntityType;
                                    joinedSourceType = childEntityType;
                                }
                                navigationInfo = new NavigationInfo(navigationType, relationLambda, joinedSource: this.CreateJoinedDataSource(joinedSourceParamType, joinedSourceType), memberExpression.Member.Name);
                                return true;
                            }
                        default:
                            throw new InvalidOperationException("Invalid navigation property");
                    }
                }
            }
            parentExpression = null;
            navigationInfo = null;
            return false;
        }


        private (NavigationType navigationType, LambdaExpression relationLambda) GetNavigationTypeAndRelationLambdaFromRelationAttribute(Type modelType, MemberInfo member, out Type parentType, out Type childType)
        {
            var relationAttribute = this.GetCustomAttribute<NavigationLinkAttribute>(modelType, member)
                                        ?? throw new InvalidOperationException($"{nameof(NavigationLinkAttribute)} is not set on member '{member.Name}'.");

            var navigationType = relationAttribute.NavigationType;

            if (!(relationAttribute.ParentKeys?.Count >= 1 && relationAttribute.ForeignKeysInChild?.Count >= 1))
                throw new InvalidOperationException("ParentKeys or ForeignKeysInChild is not set.");

            if (relationAttribute.ParentKeys.Count != relationAttribute.ForeignKeysInChild.Count)
                throw new InvalidOperationException($"ParentKeys and ForeignKeysInChild must have the same number of elements.");

            Type childModelType = modelType ?? throw new InvalidOperationException($"ReflectedType property is null for member '{member.Name}'.");

            var parentModelType = (member as PropertyInfo ?? throw new InvalidOperationException("Member is not a property")).PropertyType
                                    ?? throw new InvalidOperationException($"PropertyType is null.");

            if (parentModelType.IsGenericType && parentModelType.GetGenericTypeDefinition() == typeof(Func<>))
            {
                parentModelType = parentModelType.GetGenericArguments()[0];
            }

            if (navigationType == NavigationType.ToSingleChild)
            {
                // swapping
                (childModelType, parentModelType) = (parentModelType, childModelType);
            }

            var parentParameter = Expression.Parameter(parentModelType, "auto_p");
            var childParameter = Expression.Parameter(childModelType, "auto_c");

            var parentKeys = relationAttribute.ParentKeys;
            var foreignKeysInChild = relationAttribute.ForeignKeysInChild;

            var joinConditions = parentKeys.Zip(foreignKeysInChild, (parentKey, foreignKey) =>
            {
                var parentProperty = parentModelType.GetProperty(parentKey)
                                    ?? throw new InvalidOperationException($"Property '{parentKey}' not found in '{parentModelType.Name}'.");

                var foreignProperty = childModelType.GetProperty(foreignKey)
                                    ?? throw new InvalidOperationException($"Property '{foreignKey}' not found in '{childModelType.Name}'.");

                return Expression.Equal(Expression.Property(parentParameter, parentProperty), Expression.Property(childParameter, foreignProperty));
            }).ToList();  // Convert to list to check count safely

            // Ensure there is at least one condition before calling Aggregate()
            if (joinConditions.Count == 0)
                throw new InvalidOperationException("No valid key mappings were found.");

            // Use Aggregate only when there are multiple conditions
            var joinExpression = joinConditions.Count == 1
                ? joinConditions[0]
                : joinConditions.Aggregate(Expression.AndAlso);

            var relationLambda = Expression.Lambda(joinExpression, parentParameter, childParameter);

            parentType = parentModelType;
            childType = childModelType;
            return (navigationType, relationLambda);
        }

        private (IEntityRelation entityRelation, NavigationType navigationType) GetRelationAndTypeByEntityRelationClass(Type modelType, MemberInfo member)
        {
            var relationAttribute = this.GetCustomAttribute<NavigationPropertyAttribute>(modelType, member)
                                        ??
                                    throw new InvalidOperationException("Invalid navigation property");
            var relationType = relationAttribute.RelationType;
            var relation = Activator.CreateInstance(relationType) as IEntityRelation
                            ??
                            throw new InvalidOperationException("Invalid relation type");
            return (relation, relationAttribute.NavigationType);
        }

        private MemberInfo ResolveMemberInfo(Type modelType, MemberInfo member)
        {
            MemberInfo resolvedMemberInfo = member;
            if (modelType != null && modelType != member.ReflectedType)
            {
                resolvedMemberInfo = this.reflectionService.GetPropertyOrField(modelType, member.Name)
                                        ??
                                        resolvedMemberInfo;
            }
            return resolvedMemberInfo;
        }

        private T GetCustomAttribute<T>(Type modelType, MemberInfo member) where T : Attribute
        {
            member = this.ResolveMemberInfo(modelType, member);
            return member.GetCustomAttribute<T>();
        }

        private NavigationPropertyType GetNavigationPropertyType(Type? modelType, MemberInfo member)
        {
            var navPropAttribute = this.GetCustomAttribute<NavigationPropertyAttribute>(modelType, member);
            if (navPropAttribute != null && this.IsSupportedNavigationType(navPropAttribute.NavigationType))
                return NavigationPropertyType.EntityRelationClass;
            var relationAttribute = this.GetCustomAttribute<NavigationLinkAttribute>(modelType, member);
            if (relationAttribute != null && this.IsSupportedNavigationType(relationAttribute.NavigationType))
                return NavigationPropertyType.RelationAttribute;
            return NavigationPropertyType.None;
        }

        private bool IsSupportedNavigationType(NavigationType navigationType)
        {
            return navigationType == NavigationType.ToSingleChild || navigationType == NavigationType.ToParent || navigationType == NavigationType.ToParentOptional;
        }

        private Expression GetParentExpression(Expression currentNode, IReadOnlyCollection<Expression> expressionStack)
        {
            return this.GetMemberExpression(currentNode, expressionStack)?.Expression;
        }

        private MemberExpression GetMemberExpression(Expression currentNode, IReadOnlyCollection<Expression> expressionStack)
        {
            var node = currentNode;
            if (node is MemberExpression memberExpression &&
                !(expressionStack.Skip(1).FirstOrDefault() is InvocationExpression))
            {
                // x.NavProp
                return memberExpression;
            }
            else if (node is InvocationExpression invocationExpression &&
                        invocationExpression.Expression is MemberExpression memberExpression2)
            {
                // x.NavProp()
                return memberExpression2;
            }
            return null;
        }

        protected override Expression GetQuerySourceArgumentFromQueryMethod(Expression queryMethod)
        {
            if (queryMethod is MethodCallExpression methodCall && this.reflectionService.IsQueryMethod(queryMethod)
                    && methodCall.Arguments.Count > 0)
            {
                return methodCall.Arguments[0];
            }
            return null;
        }

        protected override Expression CreateQueryMethodCall(Expression oldQueryMethodNode, Expression wrappedQuerySourceArg)
        {
            if (oldQueryMethodNode is MethodCallExpression methodCall)
            {
                var otherArgs = methodCall.Arguments.Skip(1).ToArray();
                var allArgs = new[] { wrappedQuerySourceArg }.Concat(otherArgs).ToArray();
                return Expression.Call(methodCall.Method, allArgs);
            }
            throw new InvalidOperationException($"Method does not support '{oldQueryMethodNode.NodeType}' to create a Query Method Call");
        }

        protected override IQueryProvider GetQueryProvider()
        {
            return this.queryProvider;
        }

        protected override Type GetEnumerableEntityType(Type enumerableType)
        {
            return this.reflectionService.GetEntityTypeFromQueryableType(enumerableType);
        }


        private LambdaExpression CreateJoinedDataSource(Type parentType, Type joinedTable)
        {
            // here we will create the CreateJoinedDataSource<TParent>()
            var closedMethodInfo = createJoinedDataSourceOpenMethodInfo.MakeGenericMethod(parentType, joinedTable);
            // now we will call the method to get CreateJoinedDataSource
            var dataSourceExpression = (LambdaExpression)closedMethodInfo.Invoke(this, Array.Empty<object>());
            return dataSourceExpression;
        }

        /// <summary>
        /// Creates a joined data source expression for the specified parent type.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent entity.</typeparam>
        /// <returns>The joined data source expression.</returns>
        private Expression<Func<TParent, IQueryable<TJoined>>> CreateJoinedDataSourceGen<TParent, TJoined>()
        {
            var queryProvider = this.GetQueryProvider();
            return qs_param => QueryExtensions.DataSet<TJoined>(queryProvider);
        }
    }
}
