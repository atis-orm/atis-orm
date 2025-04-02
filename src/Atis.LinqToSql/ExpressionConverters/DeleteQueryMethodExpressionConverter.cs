using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
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

        protected override SqlExpression CreateDmSqlExpression(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource, SqlExpression[] arguments)
        {
            var deleteSqlExpression = this.SqlFactory.CreateDelete(sqlQuery, selectedDataSource);
            return deleteSqlExpression;
        }

        //protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        //{
        //    var predicate = arguments[0];
        //    sqlQuery.ApplyWhere(predicate);

        //    var deletingDataSource = sqlQuery.InitialDataSource;

        //    var deleteSqlExpression = new SqlDeleteExpression(sqlQuery, deletingDataSource);

        //    return deleteSqlExpression;
        //}
    }
}
