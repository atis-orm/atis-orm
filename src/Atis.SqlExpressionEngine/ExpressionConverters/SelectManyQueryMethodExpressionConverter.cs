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
    ///         Factory class for creating SelectMany query method expression converters.
    ///     </para>
    /// </summary>
    public class SelectManyQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SelectManyQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public SelectManyQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(Queryable.SelectMany);
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new SelectManyQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter to convert SelectMany query method expressions.
    ///     </para>
    /// </summary>
    public class SelectManyQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        private readonly ILambdaParameterToDataSourceMapper parameterMap;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SelectManyQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public SelectManyQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.parameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }


        private bool HasProjectionArgument => this.Expression.Arguments.Count >= 3;
        private int ResultSelectorArgIndex => 2;


        /// <inheritdoc />
        protected override void OnArgumentConverted(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression argument, SqlExpression convertedArgument)
        {
            var argIndex = this.Expression.Arguments.IndexOf(argument);
            if (argIndex == 1)
            {
                bool isDefaultIfEmpty = false;
                if (convertedArgument is SqlDefaultIfEmptyExpression defaultIfEmpty)
                {
                    // TODO: this might be a problem because DefaultIfEmpty can be applied on Navigation selected
                    // in SelectMany
                    convertedArgument = defaultIfEmpty.DerivedTable;
                    isDefaultIfEmpty = true;
                }

                var querySource = convertedArgument as SqlQuerySourceExpression
                                    ??
                                    throw new InvalidOperationException($"Arg-1 '{argument}' of SelectMany call is not a {nameof(SqlQuerySourceExpression)}.");
                // Below method will decide if the new data source should be added as cross join or cross apply
                // or inner join. In case if there is a where clause present in the querySource and it's linking
                // SourceQuery with this new data source then it will be added as inner join otherwise
                // cross apply or cross join.
                var newDataSource = this.SourceQuery.AddDataSourceWithJoinResolution(querySource, isDefaultIfEmpty);

                if (this.HasProjectionArgument)
                {
                    var resultSelectorArgLambda = this.GetArgumentLambda(this.ResultSelectorArgIndex);
                    // there should be 2 parameters, 1 parameter is already mapped
                    // this 2nd parameter mapping will be removed by base class `QueryMethodExpressionConverterBase`
                    // automatically
                    this.parameterMap.TrySetParameterMap(resultSelectorArgLambda.Parameters[1], newDataSource);
                }
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments)
        {
            if (this.HasProjectionArgument)
            {
                var compositeBinding = arguments[1] as SqlCompositeBindingExpression
                                        ??
                                        throw new InvalidOperationException($"3rd Argument '{this.Expression.Arguments[this.ResultSelectorArgIndex]}' of SelectMany call is converting to {arguments[1].GetType().Name} instead of {nameof(SqlCompositeBindingExpression)}.");
                sqlQuery.UpdateModelBinding(compositeBinding);
            }
            else
            {
                sqlQuery.SwitchBindingToLastDataSource();
            }
            return sqlQuery;
        }

    }
}
