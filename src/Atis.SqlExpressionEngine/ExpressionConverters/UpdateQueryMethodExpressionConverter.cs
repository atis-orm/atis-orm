using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class UpdateQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public UpdateQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression && 
                    methodCallExpression.Method.Name == nameof(QueryExtensions.Update) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new UpdateQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
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
        protected override SqlExpression CreateDmSqlExpression(SqlDerivedTableExpression sqlQuery, Guid selectedDataSource, IReadOnlyList<SqlExpression> arguments)
        {
            var columnsArgIndex = arguments.Count == 2 ? 0 : 1;
            var setClause = arguments[columnsArgIndex];
            if (!(setClause is SqlMemberInitExpression memberInit))
                throw new InvalidOperationException($"The arg-1 of the {nameof(QueryExtensions.Update)} method must be a collection of columns. Make sure arg-1 is a {nameof(MemberInitExpression)}.");

            var dataSourceToUpdate = sqlQuery.AllDataSources.Where(x=>x.Alias == selectedDataSource).FirstOrDefault()
                                        ??
                                        throw new InvalidOperationException($"The data source is not found in the query.");
            var tableToUpdate = dataSourceToUpdate.QuerySource.CastTo<SqlTableExpression>();
            
            string[] columnNames;
            SqlExpression[] values;

            columnNames = memberInit.Bindings.Select(x => tableToUpdate.GetByPropertyName(x.MemberName)).ToArray();
            values = memberInit.Bindings.Select(x => x.SqlExpression).ToArray();

            var updateSqlExpression = this.SqlFactory.CreateUpdate(sqlQuery, selectedDataSource, columnNames, values);

            return updateSqlExpression;
        }
    }
}
