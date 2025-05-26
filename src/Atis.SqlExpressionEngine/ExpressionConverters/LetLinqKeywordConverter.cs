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

    public class LetLinqKeywordConverter : QueryMethodExpressionConverterBase
    {
        public LetLinqKeywordConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }
        
        /// <inheritdoc />
        protected override SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments)
        {
            var compositeBinding = arguments[0].CastTo<SqlQueryShapeExpression>();
            sqlQuery.UpdateModelBinding(compositeBinding);
            // TODO: check if we need to mark the newly added binding as "non-projectable"
            return sqlQuery;
        }
    }
}
