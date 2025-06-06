﻿using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Metadata
{
    public interface IEntityRelation
    {
        Expression? JoinExpression { get; }
        LambdaExpression? FromParentToChild(IQueryProvider queryProvider);
        LambdaExpression? FromChildToParent(IQueryProvider queryProvider);
        Type ParentType { get; }
        Type ChildType { get; }
    }

    public interface IEntityRelation<TParent, TChild> : IEntityRelation
    {
        Expression<Func<TParent, IQueryable<TChild>>>? FromParentToChild(IQueryProvider queryProvider);
        Expression<Func<TChild, IQueryable<TParent>>>? FromChildToParent(IQueryProvider queryProvider);
    }

    public abstract class EntityRelation<TParent, TChild> : IEntityRelation<TParent, TChild>
    {
        Expression? IEntityRelation.JoinExpression => JoinExpression;
        public abstract Expression<Func<TParent, TChild, bool>>? JoinExpression { get; }
        public Type ParentType => typeof(TParent);
        public Type ChildType => typeof(TChild);
        public virtual Expression<Func<TParent, IQueryable<TChild>>>? FromParentToChild(IQueryProvider queryProvider) => null;
        public virtual Expression<Func<TChild, IQueryable<TParent>>>? FromChildToParent(IQueryProvider queryProvider) => null;
        LambdaExpression? IEntityRelation.FromChildToParent(IQueryProvider queryProvider) => this.FromChildToParent(queryProvider);
        LambdaExpression? IEntityRelation.FromParentToChild(IQueryProvider queryProvider) => this.FromParentToChild(queryProvider);
    }
}
