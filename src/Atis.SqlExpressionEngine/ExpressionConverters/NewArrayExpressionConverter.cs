using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class NewArrayExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<NewArrayExpression>
    {
        public NewArrayExpressionConverterFactory(IConversionContext context) : base(context) { }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is NewArrayExpression newArrayExpr)
            {
                converter = new NewArrayExpressionConverter(this.Context, newArrayExpr, converterStack);
                return true;
            }

            converter = null;
            return false;
        }
    }

    public class NewArrayExpressionConverter : LinqToNonSqlQueryConverterBase<NewArrayExpression>
    {
        public NewArrayExpressionConverter(IConversionContext context, NewArrayExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return new SqlCollectionExpression(convertedChildren);
        }
    }
}
