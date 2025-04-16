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
        private readonly ILambdaParameterToDataSourceMapper parameterMapper;

        public SqlFunctionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            this.parameterMapper = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        private SqlQueryExpression sqlQuerySource;

        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments.FirstOrDefault())
            {
                if (convertedExpression is SqlQueryReferenceExpression queryRef)
                {
                    var sqlQuery = queryRef.Reference;
                    // first argument might be the query argument
                    // if that's the case, we might need to the lambda parameter with
                    // this query
                    for (var i = 1; i < this.Expression.Arguments.Count; i++)
                    {
                        var argument = this.Expression.Arguments[i];
                        if (argument is UnaryExpression unaryExpression)
                            argument = unaryExpression.Operand;
                        if (argument is LambdaExpression lambda)
                        {
                            var parameter = lambda.Parameters.FirstOrDefault();
                            if (parameter != null)
                            {
                                sqlQuerySource = sqlQuery;
                                this.parameterMapper.TrySetParameterMap(parameter, sqlQuery);
                            }
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
