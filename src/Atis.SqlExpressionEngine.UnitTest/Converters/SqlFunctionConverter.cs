using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionConverters;
using Atis.SqlExpressionEngine.SqlExpressions;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.SqlExpressionEngine.UnitTest.Converters
{
    public class SqlFunctionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public SqlFunctionConverterFactory(IConversionContext context) : base(context)
        {
        }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression>? converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                Attribute.IsDefined(methodCallExpression.Method, typeof(SqlFunctionAttribute)))
            {
                converter = new SqlFunctionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }

            converter = null;
            return false;
        }
    }

    public class SqlFunctionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        public SqlFunctionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var functionName = this.Expression.Method.GetCustomAttribute<SqlFunctionAttribute>()?.SqlFunctionName
                                ??
                                throw new InvalidOperationException($"The method {this.Expression.Method.Name} does not have a SqlFunctionAttribute.");
            var sqlFunction= new SqlFunctionCallExpression(functionName, convertedChildren);
            return sqlFunction;
        }
    }
}
