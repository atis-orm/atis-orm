using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
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
    public class AggregateMethodExpressionConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterMap;
        private ParameterExpression lambdaParameterMapped;

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

        private SqlSelectExpression sourceQuery;
        private bool applyProjection;

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (this.Expression.Arguments.FirstOrDefault() == childNode)    // if 1st arg was converted
            {
                if (convertedExpression is SqlSelectExpression q)
                {
                    this.sourceQuery = q;
                    applyProjection = false;
                }
                else if (convertedExpression is SqlDerivedTableExpression d)
                {
                    this.sourceQuery = this.SqlFactory.CreateSelectQueryFromQuerySource(d);
                    applyProjection = true;
                }
                else
                    throw new InvalidOperationException($"Expected a {nameof(SqlSelectExpression)} or {nameof(SqlDerivedTableExpression)} but got {convertedExpression.GetType().Name}.");

                // mapping given query with next Lambda Parameter (if available)
                if(this.Expression.TryGetArgLambdaParameter(argIndex: 1, paramIndex: 0, out this.lambdaParameterMapped))
                {
                    this.parameterMap.TrySetParameterMap(this.lambdaParameterMapped, this.sourceQuery);
                }
            }
        }

        /// <inheritdoc/>
        public override void OnAfterVisit()
        {
            if (this.lambdaParameterMapped != null)
            {
                this.parameterMap.RemoveParameterMap(this.lambdaParameterMapped);
            }
        }

        /// <inheritdoc/>
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // skip(1) because 1st argument might be a x
            // e.g.  x.Max(y => y.Field)
            // this will be Max(x, y => y.Field)
            // so we'll skip the 1st argument
            var methodArguments = convertedChildren.Skip(1).ToArray();

            SqlExpression result;
            var functionCallExpression = this.SqlFactory.CreateFunctionCall(this.Expression.Method.Name, methodArguments);
            if (applyProjection)
            {
                var compositeBinding = this.SqlFactory.CreateCompositeBindingForSingleExpression(functionCallExpression, ModelPath.Empty);
                this.sourceQuery.ApplyProjection(compositeBinding);
                result = this.SqlFactory.ConvertSelectQueryToDeriveTable(sourceQuery);
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
