using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class NegateExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<UnaryExpression>
    {
        public NegateExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Negate)
            {
                converter = new NegateExpressionConverter(Context, unaryExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class NegateExpressionConverter : LinqToNonSqlQueryConverterBase<UnaryExpression>
    {
        public NegateExpressionConverter(IConversionContext context, UnaryExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return this.SqlFactory.CreateNegate(convertedChildren[0]);
        }
    }
}
