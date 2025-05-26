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

                // we should NOT be receiving a SqlSelectExpression, but if we are receiving one we'll convert it
                // to derived table, usually with some weird LINQ query this could happen
                // for example, x.SelectMany(y => y)
                if (convertedArgument is SqlSelectExpression selectQuery)
                {
                    convertedArgument = this.SqlFactory.ConvertSelectQueryToDeriveTable(selectQuery);
                }

                var querySource = convertedArgument.CastTo<SqlQuerySourceExpression>($"2nd Argument (Arg-1) of SelectMany must be converted to '{nameof(SqlQuerySourceExpression)}'.");
                // Below method will decide if the new data source should be added as cross join or cross apply
                // or inner join. In case if there is a where clause present in the querySource and it's linking
                // SourceQuery with this new data source then it will be added as inner join otherwise
                // cross apply or cross join.
                var newDataSource = this.SourceQuery.AddDataSourceWithJoinResolution(querySource, isDefaultIfEmpty);
                if (this.HasProjectionArgument)
                {
                    var selectorArgParam1 = this.Expression.GetArgLambdaParameterRequired(this.ResultSelectorArgIndex, paramIndex: 1);
                    this.MapParameter(selectorArgParam1, () => newDataSource);
                }
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments)
        {
            if (this.HasProjectionArgument)
            {
                var newQueryShape = arguments[1].CastTo<SqlQueryShapeExpression>();
                sqlQuery.UpdateModelBinding(newQueryShape);
            }
            else
            {
                sqlQuery.SwitchBindingToLastDataSource();
            }
            return sqlQuery;
        }

    }
}
