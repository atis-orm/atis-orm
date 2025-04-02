using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    // we modify the query variable used in the query expression with ConstantExpression this converter plugin
    // converts that

    /// <summary>
    ///     <para>
    ///         Factory to create converter that converts a <see cref="ConstantExpression"/> to a <see cref="SqlQueryExpression"/>.
    ///     </para>
    /// </summary>
    public class ConstantQueryableExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<ConstantExpression>
    {
        private readonly IReflectionService reflectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantQueryableExpressionConverterFactory"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public ConstantQueryableExpressionConverterFactory(IConversionContext context) : base(context)
        {
            this.reflectionService = this.Context.GetExtensionRequired<IReflectionService>();
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is ConstantExpression constExpr &&
                    constExpr.Value != null &&
                    this.reflectionService.IsQueryableType(constExpr.Value.GetType()) &&
                    this.reflectionService.GetQueryExpressionFromQueryable(constExpr.Value) == expression)
            {
                converter = new ConstantQueryableExpressionConverter(this.Context, constExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts a <see cref="ConstantExpression"/> to a <see cref="SqlQueryExpression"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Usually when a query variable is used within expression we preprocess those variables to <c>ConstantExpression</c>.
    ///     </para>
    ///     <code>
    ///         var q1 = from t1 in db.Table1 where t1.Field == 51 select t1;
    ///         var q2 = from t2 in db.Table2 where q1.Any(x => x.Id == t2.Id) select t2;
    ///     </code>
    ///     <para>
    ///         In above example we used <c>q1</c> within <c>q2</c>, when parsing the <c>q2</c> Expression, we see 
    ///         <c>q1</c> is there as <c>MemberExpression</c> and we don't have the actual Expression behind it.
    ///         Therefore, we preprocess the <c>q1</c> to <c>ConstantExpression</c> and replace the <c>q1</c> with actual
    ///         query expression like this,
    ///     </para>
    ///     <code>
    ///         var q2 = from t2 in db.Table2 where value(db.Table1).Where(t1 => t1.Field == 51).Any(x => x.Id == t2.Id) select t2;
    ///     </code>
    ///     <para>
    ///         This converter plugin is used to convert <c>value(db.Table1)</c> part to <c>SqlQueryExpression</c>.
    ///     </para>
    /// </remarks>
    public class ConstantQueryableExpressionConverter : LinqToSqlExpressionConverterBase<ConstantExpression>
    {
        private readonly IModel model;
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ConstantQueryableExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ConstantQueryableExpressionConverter(IConversionContext context, ConstantExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.model = this.Context.GetExtensionRequired<IModel>();
            this.reflectionService = this.Context.GetExtensionRequired<IReflectionService>();
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var entityType = this.reflectionService.GetEntityTypeFromQueryableType(this.Expression.Type);
            var tableName = this.model.GetTableName(entityType);
            var tableColumns = this.model.GetTableColumns(entityType);
            var query = this.SqlFactory.CreateQueryFromDataSource(this.SqlFactory.CreateDataSourceForTable(this.SqlFactory.CreateTable(tableName, tableColumns)));
            return query;
        }
    }
}
