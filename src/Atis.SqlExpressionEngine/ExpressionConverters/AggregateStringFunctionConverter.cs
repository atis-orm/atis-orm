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
    public class AggregateStringFunctionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public AggregateStringFunctionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpr &&
                (this.IsConcatMethodCall(methodCallExpr) ||
                this.IsJoinMethodCall(methodCallExpr)))
            {
                converter = new AggregateStringFunctionConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }

        private bool IsConcatMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(string))
                return false;
            if (methodCallExpression.Method.Name != nameof(string.Concat))
                return false;

            var groupArgument = methodCallExpression.Arguments[0];
            
            return this.IsSelectOnGroupBy(groupArgument);
        }

        private bool IsJoinMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(string))
                return false;
            if (methodCallExpression.Method.Name != nameof(string.Join))
                return false;
            if (methodCallExpression.Arguments.Count < 2)
                return false;
            
            var groupArgument = methodCallExpression.Arguments[1];

            return this.IsSelectOnGroupBy(groupArgument);
        }

        private bool IsSelectOnGroupBy(Expression groupArgument)
        {
            if (!typeof(IEnumerable<string>).IsAssignableFrom(groupArgument.Type))
                return false;

            // string.Concat( groupQuery.Select(x => x.NonGroupField) )
            // Arguments[0] = groupQuery.Select(x => x.NonGroupField)
            // Arguments[0].Type = typeof(IQueryable<string>) or typeof(IEnumerable<string>)
            // Arguments[0].NodeType = ExpressionType.Call

            if (groupArgument is MethodCallExpression selectMethodCallExpression &&
                selectMethodCallExpression.Method.Name == nameof(Enumerable.Select) &&
                selectMethodCallExpression.Arguments?.FirstOrDefault() is ParameterExpression groupByParameter &&
                groupByParameter.Type.IsGenericType && groupByParameter.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                return true;

            return false;
        }
    }

    public class AggregateStringFunctionConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        public AggregateStringFunctionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack) : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            if (this.Expression.Method.Name == nameof(string.Join))
            {
                // string.Join(", ", groupQuery.Select(x => x.NonGroupField))   
                // convertedChildren[0] = separator
                // convertedChildren[1] = x.NonGroupField
                // That's why we are passing [1] in the 1st argument of CreateStringFunction because that's the 
                // argument on which function is applied, rest of the arguments are just helping arguments.
                return this.SqlFactory.CreateStringFunction(SqlStringFunction.JoinAggregate, convertedChildren[1], new[] { convertedChildren[0] });
            }
            else if (this.Expression.Method.Name == nameof(string.Concat))
            {
                return this.SqlFactory.CreateStringFunction(SqlStringFunction.ConcatAggregate, convertedChildren[0], null);
            }

            throw new InvalidOperationException($"Aggregate string function '{this.Expression.Method.Name}' is not supported.");
        }
    }
}
