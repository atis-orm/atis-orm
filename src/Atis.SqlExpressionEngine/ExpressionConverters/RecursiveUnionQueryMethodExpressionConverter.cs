using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class RecursiveUnionQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public RecursiveUnionQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.Name == nameof(QueryExtensions.RecursiveUnion) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new RecursiveUnionQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class RecursiveUnionQueryMethodExpressionConverter : LinqToSqlQueryConverterBase<MethodCallExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper lambdaParameterMap;
        private SqlDerivedTableExpression sourceQueryAsDerivedTable;

        public RecursiveUnionQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            this.lambdaParameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc/>
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments[0])
            {
                var sourceQuery = convertedExpression as SqlSelectExpression
                                    ??
                                    throw new InvalidOperationException($"Expected {nameof(SqlSelectExpression)}, but got {convertedExpression.GetType().Name}.");

                this.sourceQueryAsDerivedTable = this.SqlFactory.ConvertSelectQueryToUnwrappableDeriveTable(sourceQuery);

                var lambdaParameterArg1 = this.Expression.GetArgLambdaParameterRequired(argIndex: 1, paramIndex: 0);
                this.lambdaParameterMap.TrySetParameterMap(lambdaParameterArg1, sourceQueryAsDerivedTable);
            }
            base.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
        }

        /// <inheritdoc/>
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];

        /// <inheritdoc/>
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sourceQuery = convertedChildren[0] as SqlSelectExpression
                        ??
                        throw new InvalidOperationException($"Arg-0: Expected {nameof(SqlSelectExpression)}, but got {convertedChildren[0].GetType().Name}.");
            var recursiveMember = convertedChildren[1] as SqlDerivedTableExpression
                                    ??
                                    throw new InvalidOperationException($"Arg-1: Expected {nameof(SqlDerivedTableExpression)}, but got {convertedChildren[1].GetType().Name}.");

            // here sourceQuery is intact because we didn't bind the sourceQuery to the lambda parameter
            // now we have the recursiveMember which has the sourceQuery used, so we need to replace
            // all the sourceQuery instances with CTE References            

            sourceQuery.ConvertToRecursiveQuery(anchorDerivedTable: this.sourceQueryAsDerivedTable, recursiveDerivedTable: recursiveMember);

            return sourceQuery;
        }
    }
}
