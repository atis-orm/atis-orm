using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{


    public class QueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            return new Queryable(this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new Queryable<TElement>(this, expression);
        }

        public object? Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }

    public class Queryable : IQueryable, IOrderedQueryable
    {
        public Type ElementType => this.Expression.Type;

        public Expression Expression { get; }

        public IQueryProvider Provider { get; }

        public Queryable(IQueryProvider provider)
        {
            this.Provider = provider;
            this.Expression = Expression.Constant(this);
        }

        public Queryable(IQueryProvider provider, Expression expression)
        {
            this.Provider = provider;
            this.Expression = expression;
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class Queryable<T> : Queryable, IQueryable<T>, IOrderedQueryable<T>
    {
        public Queryable(IQueryProvider provider)
            : base(provider)
        { }

        public Queryable(IQueryProvider provider, Expression expression)
            : base(provider, expression)
        { }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
