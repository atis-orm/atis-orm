using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class DeleteQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        public DeleteQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new DeleteQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }

        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(QueryExtensions.Delete) &&
                     methodCallExpression.Method.DeclaringType == typeof(QueryExtensions);
        }
    }
    public class DeleteQueryMethodExpressionConverter : DataManipulationQueryMethodExpressionConverterBase
    {
        public DeleteQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack) : base(context, expression, converterStack)
        {
        }

        protected override bool HasTableSelection => this.Expression.Arguments.Count == 3;

        protected override int WherePredicateArgumentIndex => this.Expression.Arguments.Count == 3 ? 2 : 1;

        protected override SqlExpression CreateDmSqlExpression(SqlDerivedTableExpression source, Guid selectedDataSource, SqlExpression[] arguments)
        {
            var deleteSqlExpression = this.SqlFactory.CreateDelete(source, selectedDataSource);
            return deleteSqlExpression;
        }
    }
}
