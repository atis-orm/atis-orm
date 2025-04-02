using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters that handle aggregate method expressions.
    ///     </para>
    /// </summary>
    public class AggregateMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="AggregateMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public AggregateMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            var aggregateMethodNames = new[] { nameof(Queryable.Count), nameof(Queryable.Max), nameof(Queryable.Min), nameof(Queryable.Sum) };
            if (expression is MethodCallExpression methodCallExpr &&
                    aggregateMethodNames.Contains(methodCallExpr.Method.Name))
            {
                converter = new AggregateMethodExpressionConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for handling aggregate method expressions.
    ///     </para>
    /// </summary>
    public class AggregateMethodExpressionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterMap;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="AggregateMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public AggregateMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.parameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        private SqlExpression sourceDatasource;

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (this.Expression.Arguments.FirstOrDefault() == childNode)    // if 1st arg was converted
            {
                this.sourceDatasource = convertedExpression;
            }
        }

        /// <inheritdoc/>
        public override void OnBeforeChildVisit(Expression childNode)
        {
            if (this.Expression.Arguments.IndexOf(childNode) == 1)      // y => y.Field argument
            {
                var arg1LambdaParameter = GetArg1LambdaParameter();
                if (arg1LambdaParameter != null)
                {
                    if (this.sourceDatasource is null)
                        throw new InvalidOperationException($"1st Argument of Aggregate method '{this.Expression.Method.Name}' is not converted yet.");
                    var dataSource = (this.sourceDatasource as SqlDataSourceReferenceExpression)?.DataSource ?? this.sourceDatasource;
                    this.parameterMap.TrySetParameterMap(arg1LambdaParameter, dataSource);
                }
            }
        }

        private ParameterExpression GetArg1LambdaParameter()
        {
            if (this.Expression.Arguments.Count > 1)        // at-least 2 arguments
            {
                var childNode = this.Expression.Arguments[1];
                var nextLambda = childNode as LambdaExpression
                                            ??
                                            (childNode as UnaryExpression)?.Operand as LambdaExpression;
                if (nextLambda != null && nextLambda.Parameters.Count > 0)
                {
                    var nextLambdaParameter = nextLambda.Parameters.First();
                    return nextLambdaParameter;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public override void OnAfterVisit()
        {
            var arg1LambdaParameter = GetArg1LambdaParameter();
            if (arg1LambdaParameter != null)
            {
                this.parameterMap.RemoveParameterMap(arg1LambdaParameter);
            }
        }

        /// <inheritdoc/>
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // skip(1) because 1st argument might be a x
            // e.g.  x.Max(y => y.Field)
            // this will be Max(x, y => y.Field)
            // so we'll skip the 1st argument
            var allArguments = convertedChildren.Take(this.Expression.Arguments.Count).ToArray();
            var firstArg = allArguments.First();
            var methodArguments = allArguments.Skip(1).ToArray();

            SqlExpression result;
            var functionCallExpression = this.SqlFactory.CreateFunctionCall(this.Expression.Method.Name, methodArguments);
            if (firstArg is SqlQueryExpression sqlQuery)
            {
                sqlQuery.ApplyProjection(functionCallExpression);
                result = sqlQuery;
            }
            else
            {
                // probably a function on group by
                result = functionCallExpression;
            }
            return result;
        }
    }
}
