using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating <see cref="TableExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class TableExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="TableExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public TableExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                    methodCallExpression.Method.IsGenericMethod &&
                    methodCallExpression.Method.GetGenericArguments().Length > 0 &&
                    methodCallExpression.Method.Name == nameof(QueryExtensions.Table) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new TableExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }
    /// <summary>
    ///     <para>
    ///         Converter class for converting method call expressions to SQL table expressions.
    ///     </para>
    /// </summary>
    public class TableExpressionConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        private readonly IModel model;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="TableExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public TableExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.model = context.GetExtensionRequired<IModel>();
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            if (!this.Expression.Method.IsGenericMethod)
                throw new System.InvalidOperationException("Table method must be generic method.");
            var genericArg0 = this.Expression.Method.GetGenericArguments().FirstOrDefault()
                                ??
                                throw new System.InvalidOperationException("Table method must have at least one generic argument.");
            var tableName = this.model.GetTableName(genericArg0);
            var tableColumns = this.model.GetTableColumns(genericArg0);
            return this.SqlFactory.CreateTable(tableName, tableColumns);
        }
    }
}
