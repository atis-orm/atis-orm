using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class ToStringConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public ToStringConverterFactory(IConversionContext context) : base(context)
        {
        }
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCall &&
                methodCall.Method.Name == nameof(ToString) &&
                ((methodCall.Arguments.Count == 1 && methodCall.Object == null) || (methodCall.Arguments.Count == 0 && methodCall.Object != null)))
            {
                converter = new ToStringConverter(this.Context, methodCall, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class ToStringConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        private readonly ISqlDataTypeFactory sqlDataTypeFactory;

        public ToStringConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            this.sqlDataTypeFactory = this.Context.GetExtensionRequired<ISqlDataTypeFactory>();
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            if (convertedChildren.Length == 0)
            {
                throw new ArgumentException("ToString method requires at least one argument.");
            }
            var sqlExpression = convertedChildren[0];
            // -1 means max length
            return this.SqlFactory.CreateCast(sqlExpression, this.sqlDataTypeFactory.CreateNonUnicodeString(-1));
        }
    }
}
