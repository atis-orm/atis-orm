using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.Preprocessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{
    public class NavigateToOnePreprocessor : NavigateToOnePreprocessorBase
    {
        private readonly IQueryProvider queryProvider;
        private readonly IReflectionService reflectionService;

        public NavigateToOnePreprocessor(IQueryProvider queryProvider, IReflectionService reflectionService)
            : base(reflectionService)
        {
            this.queryProvider = queryProvider;
            this.reflectionService = reflectionService;
        }

        protected override string? GetNavigationPropertyName(Expression currentNode, Expression[] expressionStack)
        {
            return this.GetMemberExpression(currentNode, expressionStack)?.Member.Name;
        }

        protected override Expression? GetParentExpression(Expression currentNode, Expression[] expressionStack)
        {
            return this.GetMemberExpression(currentNode, expressionStack)?.Expression;
        }

        protected override IQueryProvider GetQueryProvider() => this.queryProvider;

        protected override NavigationInfo? GetNavigationInfo(Expression currentNode, Expression[] expressionStack)
        {
            var memberExpression = this.GetMemberExpression(currentNode, expressionStack);
            if (memberExpression != null)
            {
                return this.GetNavigationInfo(memberExpression);
            }
            return null;
        }

        protected override bool IsNavigation(Expression currentNode, Expression[] expressionStack)
        {
            // node can be
            //      x.NavProp1()
            //      x.NavProp1

            var memberExpression = this.GetMemberExpression(currentNode, expressionStack);
            if (memberExpression != null)
            {
                return this.GetNavigationPropertyType(memberExpression.Expression?.Type, memberExpression.Member) != NavigationPropertyType.None;
            }
            return false;
        }

        private NavigationInfo GetNavigationInfo(MemberExpression memberExpression)
        {
            NavigationType navigationType;
            LambdaExpression? relationLambda = null;
            Expression? otherDataSource = null;

            var member = memberExpression.Member;
            var modelType = memberExpression.Expression?.Type;
            var navigationPropertyType = this.GetNavigationPropertyType(modelType, member);
            switch (navigationPropertyType)
            {
                case NavigationPropertyType.EntityRelationClass:
                    {
                        var r = GetRelationAndTypeByEntityRelationClass(modelType, member);
                        navigationType = r.navigationType;
                        // entityRelation.JoinExpression can be null for outer apply
                        if (r.entityRelation.JoinExpression != null)
                            relationLambda = r.entityRelation.JoinExpression as LambdaExpression
                                                    ?? (r.entityRelation.JoinExpression as UnaryExpression)?.Operand as LambdaExpression
                                                    ?? throw new InvalidOperationException("Invalid relation expression");
                        LambdaExpression? joinedDataSource;
                        if (navigationType == NavigationType.ToParent || navigationType == NavigationType.ToParentOptional)
                            joinedDataSource = r.entityRelation.FromChildToParent(this.queryProvider);
                        else
                            joinedDataSource = r.entityRelation.FromParentToChild(this.queryProvider);
                        if (joinedDataSource != null)
                        {
                            otherDataSource = ExpressionReplacementVisitor.Replace(joinedDataSource.Parameters[0], memberExpression.Expression, joinedDataSource.Body);
                        }
                    }
                    break;
                case NavigationPropertyType.RelationAttribute:
                    (navigationType, relationLambda) = GetNavigationTypeAndRelationLambdaFromRelationAttribute(modelType, member);
                    break;
                default:
                    throw new InvalidOperationException("Invalid navigation property");
            }
            var navigationInfo = new NavigationInfo(navigationType, relationLambda, otherDataSource);
            return navigationInfo;
        }

        private (IEntityRelation entityRelation, NavigationType navigationType) GetRelationAndTypeByEntityRelationClass(Type? modelType, MemberInfo member)
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


        private (NavigationType navigationType, LambdaExpression relationLambda) GetNavigationTypeAndRelationLambdaFromRelationAttribute(Type? modelType, MemberInfo member)
        {
            var relationAttribute = this.GetCustomAttribute<NavigationLinkAttribute>(modelType, member)
                                        ?? throw new InvalidOperationException($"{nameof(NavigationLinkAttribute)} is not set on member '{member.Name}'.");

            var navigationType = relationAttribute.NavigationType;

            if (!(relationAttribute.ParentKeys?.Length >= 1 && relationAttribute.ForeignKeysInChild?.Length >= 1))
                throw new InvalidOperationException("ParentKeys or ForeignKeysInChild is not set.");

            if (relationAttribute.ParentKeys.Length != relationAttribute.ForeignKeysInChild.Length)
                throw new InvalidOperationException($"ParentKeys and ForeignKeysInChild must have the same number of elements.");

            var childModelType = modelType ?? throw new InvalidOperationException($"ReflectedType property is null for member '{member.Name}'.");

            var parentModelType = (member as PropertyInfo ?? throw new InvalidOperationException("Member is not a property")).PropertyType
                                    ?? throw new InvalidOperationException($"PropertyType is null.");

            if (parentModelType.IsGenericType && parentModelType.GetGenericTypeDefinition() == typeof(Func<>))
            {
                parentModelType = parentModelType.GetGenericArguments()[0];
            }

            var parentParameter = Expression.Parameter(parentModelType, "p");
            var childParameter = Expression.Parameter(childModelType, "c");

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

            return (navigationType, relationLambda);
        }


        private enum NavigationPropertyType
        {
            None,
            EntityRelationClass,
            RelationAttribute
        }

        private MemberInfo ResolveMemberInfo(Type? modelType, MemberInfo member)
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


        protected virtual T? GetCustomAttribute<T>(Type? modelType, MemberInfo member) where T : Attribute
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
            return (navigationType == NavigationType.ToSingleChild || navigationType == NavigationType.ToParent || navigationType == NavigationType.ToParentOptional);
        }

        private MemberExpression? GetMemberExpression(Expression currentNode, Expression[] expressionStack)
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
    }
}
