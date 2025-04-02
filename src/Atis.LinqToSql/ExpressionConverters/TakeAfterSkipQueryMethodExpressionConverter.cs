using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters that handle the Take method following a Skip method in a LINQ query.
    ///     </para>
    /// </summary>
    public class TakeAfterSkipQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="TakeAfterSkipQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public TakeAfterSkipQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpr &&
                methodCallExpr.Method.Name == nameof(Queryable.Take) &&
                methodCallExpr.Arguments.Count > 0 && methodCallExpr.Arguments[0] is MethodCallExpression childMethodCall &&
                childMethodCall.Method.Name == nameof(Queryable.Skip))
            {
                converter = new TakeAfterSkipQueryMethodExpressionConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for handling the Take method following a Skip method in a LINQ query.
    ///     </para>
    /// </summary>
    public class TakeAfterSkipQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="TakeAfterSkipQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public TakeAfterSkipQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }


        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            var takeCountExpr = arguments[0];
            var takeCount = this.GetValue(takeCountExpr);
            sqlQuery.ApplyRowsPerPage(takeCount);
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
                throw new InvalidOperationException($"SqlExpression '{sqlExpression.NodeType}' is not valid for Skip Parameter, expected expressions are SqlParameterExpression or SqlLiteralExpression.");
            }
        }
    }
}
