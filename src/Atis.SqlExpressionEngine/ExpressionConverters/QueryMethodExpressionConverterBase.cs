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
    ///         Abstract base class for creating expression converters for query methods.
    ///     </para>
    /// </summary>
    public abstract class QueryMethodExpressionConverterFactoryBase : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="QueryMethodExpressionConverterFactoryBase"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public QueryMethodExpressionConverterFactoryBase(IConversionContext context) : base(context)
        {
        }

        /// <summary>
        ///     <para>
        ///         Determines whether the specified method call expression represents a query method call.
        ///     </para>
        /// </summary>
        /// <param name="methodCallExpression">The method call expression to check.</param>
        /// <returns><c>true</c> if the method call expression is a query method call; otherwise, <c>false</c>.</returns>
        protected abstract bool IsQueryMethodCall(MethodCallExpression methodCallExpression);

        /// <summary>
        ///     <para>
        ///         Creates the appropriate converter for the specified method call expression.
        ///     </para>
        /// </summary>
        /// <param name="methodCallExpression">The method call expression for which to create the converter.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        /// <returns>The created expression converter.</returns>
        protected abstract ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack);

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression && this.IsQueryMethodCall(methodCallExpression))
            {
                converter = this.CreateConverter(methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }


    /// <summary>
    ///     <para>
    ///         Abstract base class for converting query method expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public abstract class QueryMethodExpressionConverterBase : LinqToSqlQueryConverterBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="QueryMethodExpressionConverterBase"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        protected QueryMethodExpressionConverterBase(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var arguments = convertedChildren;
            var arg0 = arguments[0];
            var sqlQuery = arg0 as SqlSelectExpression
                            ??
                            throw new InvalidOperationException($"Expected {nameof(SqlSelectExpression)} on the stack");
            return this.Convert(sqlQuery, arguments.Skip(1).ToArray());
        }

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => this.Expression.Arguments.FirstOrDefault() == childNode;

        /// <summary>
        ///     <para>
        ///         Converts the specified SQL query and arguments to a SQL expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlQuery">The SQL query to be converted.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <returns>The converted SQL expression.</returns>
        /// <remarks>
        ///     <para>
        ///         Usually the implementers of this class should over this method for the conversion.
        ///         However, in-case if the query method initializes the instance of <see cref="SqlSelectExpression"/>
        ///         class, then <see cref="Convert(SqlExpression[])"/> method should be overridden.
        ///         But doing so will sill require to override this method as well, in that case the implementation
        ///         of this method can simply throw NotImplementedException.
        ///     </para>
        /// </remarks>
        protected abstract SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments);

        /// <summary>
        ///     <para>
        ///         Gets or sets the source query for the conversion.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This property is set by this class when the first argument of the query method is converted to <see cref="SqlSelectExpression"/>.
        ///         However, in-case if the the implementer of this class is directly overriding <see cref="Convert(SqlExpression[])"/> method,
        ///         then it's implementer's responsibility to set this property after initializing the <see cref="SqlSelectExpression"/> instance.
        ///     </para>
        /// </remarks>
        protected SqlSelectExpression SourceQuery { get; set; }

        /// <inheritdoc />
        public override sealed void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments.FirstOrDefault())
            {
                if (this.SourceQuery != null)
                    throw new InvalidOperationException($"SourceQuery must be null at this point");

                SqlSelectExpression sqlQuery = convertedExpression as SqlSelectExpression
                                                ??
                                                throw new InvalidOperationException($"Expected {nameof(SqlSelectExpression)} on the stack, but got {convertedExpression.GetType()}");

                this.SourceQuery = sqlQuery;

                this.OnSourceQueryCreated();
            }
            else
                this.OnArgumentConverted(childConverter, childNode, convertedExpression);
        }

        /// <summary>
        ///     <para>
        ///         Called when an argument has been converted.
        ///     </para>
        /// </summary>
        /// <param name="childConverter">The child converter responsible for the conversion.</param>
        /// <param name="argument">The original argument expression.</param>
        /// <param name="converterArgument">The converted argument expression.</param>
        /// <remarks>
        ///     <para>
        ///         Since this class has overridden the <see cref="OnConversionCompletedByChild(ExpressionConverterBase{Expression, SqlExpression}, System.Linq.Expressions.Expression, SqlExpression)"/>
        ///         method as sealed, therefore, the implementers of this class should override this method
        ///         to inject the logic when an argument has been converted.
        ///     </para>
        /// </remarks>
        protected virtual void OnArgumentConverted(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression argument, SqlExpression converterArgument)
        {
            // do nothing
        }

        /// <summary>
        ///     <para>
        ///         Called when the source query has been created.
        ///     </para>
        /// </summary>
        protected virtual void OnSourceQueryCreated()
        {
            for (var i = 1; i < this.Expression.Arguments.Count; i++)
            {
                if (this.Expression.TryGetArgLambda(i, out var argLambda))
                    this.MapLambdaParameter(argLambda);
            }
        }

        /// <summary>
        ///     <para>
        ///         Maps the lambda parameter to the data source for the specified argument index.
        ///     </para>
        /// </summary>
        /// <param name="argLambda"></param>
        protected void MapLambdaParameter(LambdaExpression argLambda)
        {
            if (argLambda?.Parameters.Count > 0)
            {
                var firstParam = argLambda.Parameters.First();
                if (this.ReflectionService.IsGroupingType(firstParam.Type))
                {
                    this.MapParameter(firstParam, () => this.SourceQuery);
                }
                else
                {
                    this.MapParameter(firstParam, () => this.SourceQuery.GetQueryShapeForFieldMapping());
                }
            }
        }

    }
}
