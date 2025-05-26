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

    public class SqlFunctionConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        public SqlFunctionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        private SqlSelectExpression sqlQuerySource;

        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments.FirstOrDefault())
            {
                if (convertedExpression is SqlSelectExpression sqlQuery)
                {
                    // first argument might be the query argument
                    // if that's the case, we might need to map the lambda parameter with
                    // this query
                    for (var i = 1; i < this.Expression.Arguments.Count; i++)
                    {
                        if (this.Expression.TryGetArgLambdaParameter(i, 0, out var parameterExpression))
                        {
                            // we have a lambda expression
                            // we need to map the parameter with the query
                            // and set the sqlQuerySource
                            this.sqlQuerySource = sqlQuery;
                            this.MapParameter(parameterExpression, () => sqlQuery.GetQueryShapeForFieldMapping());
                        }
                    }
                }
            }
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            IEnumerable<SqlExpression> functionArguments;
            if (this.sqlQuerySource != null)
                functionArguments = convertedChildren.Skip(1);
            else
                functionArguments = convertedChildren;
            var functionName = this.Expression.Method.GetCustomAttribute<SqlFunctionAttribute>()?.SqlFunctionName
                                ??
                                throw new InvalidOperationException($"The method {this.Expression.Method.Name} does not have a SqlFunctionAttribute.");
            var sqlFunction = new SqlFunctionCallExpression(functionName, functionArguments.ToArray());
            return sqlFunction;
        }
    }
}
