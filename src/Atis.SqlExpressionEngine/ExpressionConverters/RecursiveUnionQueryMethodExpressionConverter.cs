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
        private SqlDerivedTableExpression sourceQueryAsDerivedTable;

        public RecursiveUnionQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        /// <inheritdoc/>
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments[0])
            {
                var sourceQuery = convertedExpression.CastTo<SqlSelectExpression>();

                var sourceQueryCopy = sourceQuery.CreateCopy();
                this.sourceQueryAsDerivedTable = this.SqlFactory.ConvertSelectQueryToUnwrappableDeriveTable(sourceQueryCopy);

                var lambdaParameterArg1 = this.Expression.GetArgLambdaParameterRequired(argIndex: 1, paramIndex: 0);
                this.MapParameter(lambdaParameterArg1, () => sourceQueryAsDerivedTable);
            }
            base.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
        }

        /// <inheritdoc/>
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];

        /// <inheritdoc/>
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sourceQuery = convertedChildren[0].CastTo<SqlSelectExpression>();
            var recursiveMember = convertedChildren[1].CastTo<SqlDerivedTableExpression>();

            // here sourceQuery is intact because we didn't bind the sourceQuery to the lambda parameter
            // now we have the recursiveMember which has the sourceQuery used, so we need to replace
            // all the sourceQuery instances with CTE References            

            // TODO: check if we could remove the anchorDerivedTable parameter
            // and implement the logic in the SqlSelectExpression to convert itself
            // to anchor
            sourceQuery.ConvertToRecursiveQuery(anchorDerivedTable: this.sourceQueryAsDerivedTable, recursiveDerivedTable: recursiveMember);

            return sourceQuery;
        }
    }
}
