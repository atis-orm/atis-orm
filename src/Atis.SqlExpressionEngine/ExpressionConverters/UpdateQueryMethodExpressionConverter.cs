using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class UpdateQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        public UpdateQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new UpdateQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(QueryExtensions.Update) &&
                     methodCallExpression.Method.DeclaringType == typeof(QueryExtensions);
        }
    }
    public class UpdateQueryMethodExpressionConverter : DataManipulationQueryMethodExpressionConverterBase
    {
        public UpdateQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack) : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override bool HasTableSelection => this.Expression.Arguments.Count == 4;
        /// <inheritdoc />
        protected override int WherePredicateArgumentIndex => this.Expression.Arguments.Count == 4 ? 3 : 2;
        /// <inheritdoc />
        protected override SqlExpression CreateDmSqlExpression(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource, SqlExpression[] arguments)
        {
            var columnsArgIndex = arguments.Length == 2 ? 0 : 1;
            var columns = arguments[columnsArgIndex];
            if (!(columns is SqlCollectionExpression sqlCollectionExpression))
                throw new InvalidOperationException($"The arg-1 of the {nameof(QueryExtensions.Update)} method must be a collection of columns. Make sure arg-1 is a {nameof(MemberInitExpression)}.");

            SqlColumnExpression[] sqlColumns;
            try
            {
                sqlColumns = sqlCollectionExpression.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"The arg-1 of the {nameof(QueryExtensions.Update)} is a {nameof(SqlCollectionExpression)}, however, property {nameof(SqlCollectionExpression.SqlExpressions)} is not {nameof(SqlColumnExpression)}.", ex);
            }

            var columnNames = sqlColumns.Select(x => x.ColumnAlias).ToArray();
            var values = sqlColumns.Select(x => x.ColumnExpression).ToArray();

            var updateSqlExpression = this.SqlFactory.CreateUpdate(sqlQuery, selectedDataSource, columnNames, values);

            return updateSqlExpression;
        }
    }
}
