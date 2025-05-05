using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters for paging query methods.
    ///     </para>
    /// </summary>
    public class PagingQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="PagingQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public PagingQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(QueryExtensions.Paging) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions);
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new PagingQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }
    }


    /// <summary>
    ///     <para>
    ///         Converter class for converting paging query method expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public class PagingQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="PagingQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public PagingQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments)
        {
            var pageNumberExpr = arguments[0];
            var pageSizeExpr = arguments[1];

            var pageNumber = this.GetValue(pageNumberExpr);
            var pageSize = this.GetValue(pageSizeExpr);

            sqlQuery.ApplyPaging(pageNumber, pageSize);
            return sqlQuery;
        }

        private int GetValue(SqlExpression sqlExpression)
        {
            if (sqlExpression is SqlParameterExpression sqlParameterExpression &&
                sqlParameterExpression.Value is int value)
            {
                return value;
            }
            else if (sqlExpression is SqlLiteralExpression sqlLiteralExpression &&
                     sqlLiteralExpression.LiteralValue is int value2)
            {
                return value2;
            }
            else
            {
                throw new InvalidOperationException($"SqlExpression '{sqlExpression.NodeType}' is not valid for Paging Parameter, expected expressions are SqlParameterExpression or SqlLiteralExpression.");
            }
        }
    }
}
