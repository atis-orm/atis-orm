using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class BulkInsertConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public BulkInsertConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCall &&
                methodCall.Method.Name == nameof(QueryExtensions.BulkInsert) &&
                methodCall.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new BulkInsertConverter(this.Context, methodCall, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }
    public class BulkInsertConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        private readonly IModel model;

        public BulkInsertConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            this.model = this.Context.GetExtensionRequired<IModel>();
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var derivedTable = convertedChildren[0].CastTo<SqlDerivedTableExpression>("The first child must be a derived table for bulk insert.");
            var tableType = this.ReflectionService.GetEntityTypeFromQueryableType(this.Expression.Arguments[0].Type);
            var sqlTable = this.model.GetSqlTable(tableType);
            var tableColumns = this.model.GetTableColumns(tableType);
            var bulkInsertExpression = this.SqlFactory.CreateInsertInto(sqlTable, tableColumns, derivedTable);
            return bulkInsertExpression;
        }
    }
}
