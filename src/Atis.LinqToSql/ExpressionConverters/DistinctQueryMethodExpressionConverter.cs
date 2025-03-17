using Atis.Expressions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
{
    public class DistinctQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        public DistinctQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new DistinctQueryMethodExpressionConverter(Context, methodCallExpression, converterStack);
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression) 
            => methodCallExpression.Method.Name == nameof(Queryable.Distinct);
    }

    public class DistinctQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        public DistinctQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack) : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            sqlQuery.ApplyDistinct();
            return sqlQuery;
        }
    }
}
