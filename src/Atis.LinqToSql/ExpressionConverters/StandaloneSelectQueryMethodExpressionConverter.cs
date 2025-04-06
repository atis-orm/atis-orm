﻿using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
{
    public class StandaloneSelectQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public StandaloneSelectQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                    methodCallExpression.Method.Name == nameof(QueryExtensions.Select) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new StandaloneSelectQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class StandaloneSelectQueryMethodExpressionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        public StandaloneSelectQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            if (sourceExpression == this.Expression.Arguments[0])
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
            // convertedChildren[0] is dummy (for IQueryProvider)
            var sqlQuery = this.SqlFactory.CreateQueryFromSelect(convertedChildren[1]);
            return sqlQuery;
        }
    }
}
