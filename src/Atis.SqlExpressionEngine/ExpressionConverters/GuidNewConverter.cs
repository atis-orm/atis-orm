using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class GuidNewConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public GuidNewConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCall &&
                methodCall.Method.Name == nameof(Guid.NewGuid) &&
                methodCall.Method.DeclaringType == typeof(Guid))
            {
                converter = new GuidNewConverter(Context, methodCall, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class GuidNewConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        public GuidNewConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return this.SqlFactory.CreateNewGuid();
        }
    }
}
