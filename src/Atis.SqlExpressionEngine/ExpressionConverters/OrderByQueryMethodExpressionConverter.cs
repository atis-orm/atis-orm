using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters for OrderBy query methods.
    ///     </para>
    /// </summary>
    public class OrderByQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="OrderByQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public OrderByQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(Queryable.OrderBy) ||
                    methodCallExpression.Method.Name == nameof(Queryable.ThenBy) ||
                    methodCallExpression.Method.Name == nameof(Queryable.OrderByDescending) ||
                    methodCallExpression.Method.Name == nameof(Queryable.ThenByDescending) ||
                    (methodCallExpression.Method.Name == nameof(QueryExtensions.OrderByDesc) &&
                        methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
                    ;
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new OrderByQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting OrderBy query method expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public class OrderByQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="OrderByQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public OrderByQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            var orderByPart = arguments[0];
            bool ascending;
            if (this.Expression.Method.Name == nameof(Queryable.OrderByDescending) ||
                 this.Expression.Method.Name == nameof(Queryable.ThenByDescending) ||
                 this.Expression.Method.Name == nameof(QueryExtensions.OrderByDesc))
                ascending = false;      // descending
            else
                ascending = true;
            SqlOrderByExpression orderByExpression = this.SqlFactory.CreateOrderBy(orderByPart, ascending);
            sqlQuery.ApplyOrderBy(orderByExpression);
            return sqlQuery;
        }
    }
}
