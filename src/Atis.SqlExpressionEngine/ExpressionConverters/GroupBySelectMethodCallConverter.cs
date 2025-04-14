using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Linq;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class GroupBySelectMethodCallConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public GroupBySelectMethodCallConverterFactory(IConversionContext context) : base(context)
        {
        }


        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpr &&
                methodCallExpr.Method.Name == nameof(Enumerable.Select))
            {
                if (methodCallExpr.Arguments.Count == 2)
                {
                    var firstArgument = methodCallExpr.Arguments[0];
                    if (firstArgument.Type.IsGenericType && firstArgument.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                    {
                        converter = new GroupBySelectMethodCallConverter(this.Context, methodCallExpr, converterStack);
                        return true;
                    }
                }
            }
            converter = null;
            return false;
        }
    }

    public class GroupBySelectMethodCallConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterMapper;

        public GroupBySelectMethodCallConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            this.parameterMapper = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            // Select(x, y => y.NonGroupingField)

            if (childNode == this.Expression.Arguments[0])      // childNode = x
            {
                SqlQueryExpression sqlQuery = (convertedExpression as SqlDataSourceReferenceExpression)?.DataSource as SqlQueryExpression
                                                ??
                                                throw new InvalidOperationException($"The first argument of the Select method must be a SqlQueryExpression. The provided argument is of type {convertedExpression.GetType()}.");

                var selector = this.Expression.Arguments[1];    // selector = y => y.NonGroupingField
                if (selector is UnaryExpression ue)
                    selector = ue.Operand;
                var lambda = selector as LambdaExpression
                                ??
                                throw new InvalidOperationException($"The selector argument of the Select method must be a LambdaExpression. The provided argument is of type {selector.GetType()}.");
                if (lambda.Parameters.Count == 0)
                    throw new InvalidOperationException($"The selector argument of the Select method must have at least one parameter. The provided argument has {lambda.Parameters.Count} parameters.");

                this.parameterMapper.TrySetParameterMap(lambda.Parameters[0], sqlQuery);
            }
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return convertedChildren[1];
        }
    }
}
