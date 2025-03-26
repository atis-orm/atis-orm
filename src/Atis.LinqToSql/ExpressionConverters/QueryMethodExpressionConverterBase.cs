using Atis.Expressions;
using Atis.LinqToSql.ContextExtensions;
using Atis.LinqToSql.ExpressionExtensions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
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
    public abstract class QueryMethodExpressionConverterBase : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Gets the parameter map for lambda expressions.
        ///     </para>
        /// </summary>
        protected ILambdaParameterToDataSourceMapper ParameterMap { get; }

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
            this.ParameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var arguments = convertedChildren;
            var arg0 = arguments[0];
            if (arg0 is SqlDataSourceReferenceExpression dsRef)
                arg0 = dsRef.DataSource;
            var sqlQuery = arg0 as SqlQueryExpression
                           ??
                           throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} on the stack");
            return this.Convert(sqlQuery, arguments.Skip(1).ToArray());
        }

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
        ///         However, in-case if the query method initializes the instance of <see cref="SqlQueryExpression"/>
        ///         class, then <see cref="Convert(IReadOnlyStack{SqlExpression})"/> method should be overridden.
        ///         But doing so will sill require to override this method as well, in that case the implementation
        ///         of this method can simply throw NotImplementedException.
        ///     </para>
        /// </remarks>
        protected abstract SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments);

        /// <summary>
        ///     <para>
        ///         Gets or sets the source query for the conversion.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This property is set by this class when the first argument of the query method is converted to <see cref="SqlQueryExpression"/>.
        ///         However, in-case if the the implementer of this class is directly overriding <see cref="Convert(IReadOnlyStack{SqlExpression})"/> method,
        ///         then it's implementer's responsibility to set this property after initializing the <see cref="SqlQueryExpression"/> instance.
        ///     </para>
        /// </remarks>
        protected SqlQueryExpression SourceQuery { get; set; }

        /// <inheritdoc />
        public override sealed void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments.FirstOrDefault())
            {
                SqlQueryExpression sqlQuery = (convertedExpression as SqlDataSourceReferenceExpression)?.DataSource as SqlQueryExpression
                                                ??
(                                                (convertedExpression as SqlDataSourceReferenceExpression)?.DataSource as SqlDataSourceExpression)?.QuerySource as SqlQueryExpression
                                                ??
                                                convertedExpression as SqlQueryExpression;
                
                if (this.SourceQuery == null)
                {
                    this.SourceQuery = sqlQuery;
                }

                if (sqlQuery != null)
                {
                    this.OnSourceQueryCreated();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($" ****** WARNING: Query Method Converter = {this.GetType().Name}, First argument was not translated to {nameof(SqlQueryExpression)}, instead it was translated to {convertedExpression.GetType().Name} ****** ");
                }
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
                this.MapLambdaParameter(this.SourceQuery, i);
            }
        }

        /// <summary>
        ///     <para>
        ///         Maps the lambda parameter to the data source for the specified argument index.
        ///     </para>
        /// </summary>
        /// <param name="sqlQuery">The SQL query to which the parameter is mapped.</param>
        /// <param name="argIndex">The index of the argument.</param>
        protected void MapLambdaParameter(SqlQueryExpression sqlQuery, int argIndex)
        {
            LambdaExpression argLambda = GetArgumentLambda(argIndex);
            if (argLambda?.Parameters.Count > 0)
            {
                var firstParam = argLambda.Parameters.First();
                ParameterMap.TrySetParameterMap(firstParam, sqlQuery);
            }
        }

        /// <summary>
        ///     <para>
        ///         Gets the lambda expression for the specified argument index.
        ///     </para>
        /// </summary>
        /// <param name="argIndex">The index of the argument.</param>
        /// <returns>The lambda expression for the specified argument index.</returns>
        protected LambdaExpression GetArgumentLambda(int argIndex)
        {
            var arg = this.Expression.Arguments[argIndex];
            var argLambda = (arg as UnaryExpression)?.Operand as LambdaExpression
                                                    ??
                                                    arg as LambdaExpression;
            return argLambda;
        }

        /// <inheritdoc />
        public sealed override void OnAfterVisit()
        {
            for(var i = 0; i < this.Expression.Arguments.Count; i++)
            {
                var argLambda = this.GetArgumentLambda(i);
                if (argLambda != null)
                {
                    foreach (var param in argLambda.Parameters)
                    {
                        ParameterMap.RemoveParameterMap(param);
                    }
                }
            }
        }
    }
}
