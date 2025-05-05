using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters for query methods such as Where, Having, and WhereOr.
    ///     </para>
    /// </summary>
    public class WhereQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="WhereQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public WhereQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(Queryable.Where)
                            ||
                    (
                        (
                            methodCallExpression.Method.Name == nameof(QueryExtensions.Having)
                                ||
                            methodCallExpression.Method.Name == nameof(QueryExtensions.WhereOr)
                        )
                    &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions)
                    );
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new WhereQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting query method expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public class WhereQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="WhereQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public WhereQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }


        /// <inheritdoc />
        protected override SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments)
        {
            var predicate = arguments[0];
            bool useOrOperator = this.Expression.Method.Name == nameof(QueryExtensions.WhereOr) || this.Expression.Method.Name == nameof(QueryExtensions.HavingOr);
            if (this.Expression.Method.Name == nameof(QueryExtensions.Having) || this.Expression.Method.Name == nameof(QueryExtensions.HavingOr)
                   || (this.Expression.Arguments[0] is MethodCallExpression methodCall && methodCall.Method.Name == nameof(Queryable.GroupBy)))
                sqlQuery.ApplyHaving(predicate, useOrOperator);
            else
                sqlQuery.ApplyWhere(predicate, useOrOperator);
            return sqlQuery;
        }
    }
}
