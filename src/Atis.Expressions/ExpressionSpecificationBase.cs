using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{
    public interface IExpressionSpecification
    {
        bool IsSatisfiedBy(object entity);
        LambdaExpression ToExpression();
    }

    public abstract class ExpressionSpecificationBase<T> : IExpressionSpecification
    {
        public abstract Expression<Func<T, bool>> ToExpression();

        private Func<T, bool> predicate;
        protected virtual Func<T, bool> Predicate
        {
            get
            {
                if (predicate is null)
                {
                    predicate = this.ToExpression().Compile();
                }
                return predicate;
            }
        }

        public virtual bool IsSatisfiedBy(T entity)
        {
            return Predicate(entity);
        }

        /// <inheritdoc />
        LambdaExpression IExpressionSpecification.ToExpression() => this.ToExpression();
        /// <inheritdoc />
        //public bool IsSatisfiedBy(object entity) => this.IsSatisfiedBy((T)entity);
        bool IExpressionSpecification.IsSatisfiedBy(object entity) => this.IsSatisfiedBy((T)entity);
    }
}
