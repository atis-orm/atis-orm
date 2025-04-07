using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Preprocessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public class NavigateToManyPreprocessor : NavigateToManyPreprocessorBase
    {
        private readonly IQueryProvider queryProvider;
        private readonly IReflectionService reflectionService;

        public NavigateToManyPreprocessor(IQueryProvider queryProvider, IReflectionService reflectionService)
        {
            this.queryProvider = queryProvider;
            this.reflectionService = reflectionService;
        }

        protected override IQueryable<T> CreateQuery<T>()
        {
            return new Queryable<T>(queryProvider);
        }

        protected override Type? GetEntityType(Expression navigationNode)
        {
            var type = navigationNode.Type;
            if (type.IsGenericType && typeof(IQueryable).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[0];
            }
            return null;
        }

        protected override NavigationInfo? GetNavigationInfo(Expression node)
        {
            if (node is MemberExpression memberExpression)
            {
                var member = this.ResolveMember(memberExpression);
                var relationAttribute = member.GetCustomAttribute<NavigationPropertyAttribute>()
                                            ??
                                        throw new InvalidOperationException("Invalid navigation property");
                var relationType = relationAttribute.RelationType;
                var relation = Activator.CreateInstance(relationType) as IEntityRelation
                                ??
                                throw new InvalidOperationException("Invalid relation type");
                LambdaExpression? relationLambda = null;
                if (relation.JoinExpression != null)
                    relationLambda = relation.JoinExpression as LambdaExpression
                                            ?? (relation.JoinExpression as UnaryExpression)?.Operand as LambdaExpression
                                            ?? throw new InvalidOperationException("Invalid relation expression");
                LambdaExpression? joinedSource = null;
                if (relation.FromParentToChild != null)
                {
                    joinedSource = relation.FromParentToChild(this.queryProvider);
                }
                var navigationInfo = new NavigationInfo(relationAttribute.NavigationType, relationLambda, joinedSource);
                return navigationInfo;
            }
            return null;
        }

        protected override bool IsNavigationExpression(Expression node)
        {
            if (node is MemberExpression memberExpression)
            {
                var member = this.ResolveMember(memberExpression);
                if (member != null)
                {
                    var navPropAttribute = member.GetCustomAttribute<NavigationPropertyAttribute>();
                    return navPropAttribute != null &&
                            navPropAttribute.NavigationType == NavigationType.ToChildren;
                }
            }
            return false;
        }

        private MemberInfo ResolveMember(MemberExpression memberExpression)
        {
            var resolvedMember = memberExpression.Member;
            if (memberExpression.Expression != null && memberExpression.Expression.Type != resolvedMember.ReflectedType)
            {
                resolvedMember = this.reflectionService.GetPropertyOrField(memberExpression.Expression.Type, resolvedMember.Name);
            }
            return resolvedMember;
        }
    }
}
