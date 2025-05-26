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
        private readonly IReflectionService reflectionService;

        public AggregateStringFunctionConverterFactory(IConversionContext context) : base(context)
        {
            this.reflectionService = this.Context.GetExtensionRequired<IReflectionService>();
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
                this.reflectionService.IsGroupingType(groupByParameter.Type))
                return true;

            return false;
        }
    }

    public class AggregateStringFunctionConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        public AggregateStringFunctionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack) : base(context, expression, converterStack)
        {
        }

        public override bool TryCreateChildConverter(Expression childNode, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> childConverter)
        {
            if ((this.Expression.Method.Name == nameof(string.Join) && childNode == this.Expression.Arguments[1]) ||
                (this.Expression.Method.Name == nameof(string.Concat) && childNode == this.Expression.Arguments[0]))
            {
                childConverter = new GroupBySelectMethodCallForStringAggregateConverter(this.Context, (MethodCallExpression)childNode, converterStack);
                return true;
            }
            return base.TryCreateChildConverter(childNode, converterStack, out childConverter);
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


        private class GroupBySelectMethodCallForStringAggregateConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
        {
            public GroupBySelectMethodCallForStringAggregateConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
            {
            }

            /// <inheritdoc />
            public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
            {
                // Select(x, y => y.NonGroupingField)

                if (childNode == this.Expression.Arguments[0])      // childNode = x
                {
                    var sqlQuery = convertedExpression.CastTo<SqlSelectExpression>();

                    var arg1Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 1, paramIndex: 0);
                    this.MapParameter(arg1Param0, () => sqlQuery.GetQueryShapeForFieldMapping());
                }
            }

            /// <inheritdoc />
            public override SqlExpression Convert(SqlExpression[] convertedChildren)
            {
                return convertedChildren[1];
            }
        }
    }
}
