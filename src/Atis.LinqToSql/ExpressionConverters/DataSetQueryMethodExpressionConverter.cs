using Atis.Expressions;
using Atis.LinqToSql.ContextExtensions;
using Atis.LinqToSql.Infrastructure;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters for DataSet query method expressions.
    ///     </para>
    /// </summary>
    public class DataSetQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="DataSetQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public DataSetQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                    methodCallExpression.Method.Name == nameof(QueryExtensions.DataSet) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new DataSetQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting DataSet query method expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public class DataSetQueryMethodExpressionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression> 
    {
        private readonly IModel model;
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="DataSetQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public DataSetQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.model = context.GetExtension<IModel>();
            this.reflectionService = context.GetExtension<IReflectionService>();
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            if (this.Expression.Arguments.FirstOrDefault() == sourceExpression)
            {
                convertedExpression = this.SqlFactory.CreateLiteral("dummy");
                return true;
            }
            convertedExpression = null;
            return false;
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sourceType = this.reflectionService.GetEntityTypeFromQueryableType(this.Expression.Type);
            var tableName = this.model.GetTableName(sourceType);
            var tableColumns = this.model.GetTableColumns(sourceType);
            var table = this.SqlFactory.CreateTable(tableName, tableColumns);
            var tableDataSource = this.SqlFactory.CreateDataSourceForTable(table);
            var result = this.SqlFactory.CreateQueryFromDataSource(tableDataSource);
            return result;
        }
    }
}
