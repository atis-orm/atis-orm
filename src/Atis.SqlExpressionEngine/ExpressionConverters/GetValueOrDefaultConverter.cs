using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class GetValueOrDefaultConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        private readonly static HashSet<Type> supportedTypes = new HashSet<Type>(new []
        {
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
        });

        public GetValueOrDefaultConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCall &&
                methodCall.Method.Name == "GetValueOrDefault" &&
                methodCall.Method.DeclaringType.IsGenericType &&
                methodCall.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                supportedTypes.Contains(methodCall.Type))
            {
                converter = new GetValueOrDefaultConverter(this.Context, methodCall, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class GetValueOrDefaultConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        public GetValueOrDefaultConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc/>
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            SqlExpression defaultValue;
            if (this.Expression.Type == typeof(bool))
            {
                defaultValue = this.SqlFactory.CreateLiteral(false);
            }
            else
            {
                defaultValue = this.SqlFactory.CreateLiteral(0);
            }
            return this.SqlFactory.CreateBinary(convertedChildren[0], defaultValue, SqlExpressionType.Coalesce);
        }
    }
}
