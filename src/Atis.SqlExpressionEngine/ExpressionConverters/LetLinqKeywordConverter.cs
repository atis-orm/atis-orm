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
    public class LetLinqKeywordConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public LetLinqKeywordConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (converterStack.Length > 0 &&
                expression is MethodCallExpression methodCall &&                // a method call
                methodCall.Method.Name == nameof(Queryable.Select) &&           // .Select
                (methodCall.Method.DeclaringType == typeof(Queryable) ||        // Select method should be from Queryable 
                methodCall.Method.DeclaringType == typeof(Enumerable)) &&       //  or Enumerable
                methodCall.Arguments.Count == 2 &&                              // .Select(arg0, arg1)
                methodCall.Arguments[1] is UnaryExpression ue &&                // arg0 must be Quote
                ue.Operand is LambdaExpression lambda &&                        // and Quote must be wrapping a Lambda
                lambda.Parameters.Count == 1 &&                                 // p1 => 
                lambda.Body is NewExpression newExpression &&                   // p1 => new { 
                newExpression.Arguments[0] == lambda.Parameters[0])             // p1 => new { p1, ....
            {
                converter = new LetLinqKeywordConverter(this.Context, methodCall, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class LetLinqKeywordConverter : LinqToSqlQueryConverterBase<MethodCallExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper lambdaParameterMap;
        private ParameterExpression lambdaParameterMapped;

        public LetLinqKeywordConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            lambdaParameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments[0])
            {
                // query converted
                this.lambdaParameterMapped = this.Expression.GetArgLambdaParameterRequired(1, 0);
                this.lambdaParameterMap.TrySetParameterMap(this.lambdaParameterMapped, convertedExpression);
            }
            base.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
        }

        /// <inheritdoc />
        public override void OnAfterVisit()
        {
            if (this.lambdaParameterMapped != null)
                this.lambdaParameterMap.RemoveParameterMap(this.lambdaParameterMapped);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sqlQuery = convertedChildren[0] as SqlSelectExpression
                            ??
                            throw new InvalidOperationException($"The first argument of the Select method must be a {nameof(SqlSelectExpression)}, but got {convertedChildren[0].GetType().Name}.");
            var compositeBinding = convertedChildren[1] as SqlCompositeBindingExpression
                                   ??
                                   throw new InvalidOperationException($"The second argument of the Select method must be a {nameof(SqlCompositeBindingExpression)}, but got {convertedChildren[1].GetType().Name}.");
            sqlQuery.UpdateModelBinding(compositeBinding);
            sqlQuery.MarkModelBindingAsNonProjectable(compositeBinding.Bindings[1].ModelPath);
            return sqlQuery;
        }

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];
    }
}
