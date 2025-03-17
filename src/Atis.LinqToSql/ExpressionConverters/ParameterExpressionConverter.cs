using Atis.Expressions;
using Atis.LinqToSql.ContextExtensions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating <see cref="ParameterExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class ParameterExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<ParameterExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ParameterExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public ParameterExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <summary>
        ///     <para>
        ///         Attempts to create an expression converter for the specified source expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">The source expression for which the converter is being created.</param>
        /// <param name="converterStack">The current stack of converters in use, which may influence the creation of the new converter.</param>
        /// <param name="converter">When this method returns, contains the created expression converter if the creation was successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a suitable converter was successfully created; otherwise, <c>false</c>.</returns>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is ParameterExpression parameterExpression)
            {
                converter = new ParameterExpressionConverter(this.Context, parameterExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts <see cref="ParameterExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class ParameterExpressionConverter : LinqToSqlExpressionConverterBase<ParameterExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterMapper;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ParameterExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ParameterExpressionConverter(IConversionContext context, ParameterExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.parameterMapper = context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <summary>
        ///     <para>
        ///         Gets the data source associated with the current parameter expression.
        ///     </para>
        /// </summary>
        /// <returns>The associated data source as a <see cref="SqlExpression"/>.</returns>
        protected virtual SqlExpression GetDataSourceByParameterExpression()
        {
            return this.parameterMapper.GetDataSourceByParameterExpression(this.Expression);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var sqlExpression = this.GetDataSourceByParameterExpression()
                            ??
                            throw new InvalidOperationException($"No SqlExpression found for '{this.Expression}'");
            var isLeafNode = !(this.ParentExpression is MemberExpression);
            // isLeafNode is true when the ParameterExpression is selected alone
            if (!(sqlExpression is SqlQueryExpression || sqlExpression is SqlDataSourceExpression))
                throw new InvalidOperationException($"'{sqlExpression}' is neither SqlQueryExpression nor SqlDataSourceExpression");
            SqlExpression result;
            if (isLeafNode)
            {
                if (sqlExpression is SqlQueryExpression sqlQuery && !sqlQuery.IsMultiDataSourceQuery)
                {
                    var scalarVal = sqlQuery.GetScalarColumnExpression();
                    if (scalarVal != null)
                        return scalarVal;
                }
                result = new SqlDataSourceReferenceExpression(sqlExpression);
            }
            else
                result = sqlExpression;

            // In-case if ParameterExpression is representing a Multi Data Source Query, then it's impossible
            // that single direct ParameterExpression will ever translate into scalar column,
            // in that case, the direct ParameterExpression selection will cause all the columns in all the
            // data sources to be selected in projection.
            // Which means either the ParameterExpression will be a part of MemberExpression or it will be
            // selected by user directly in Projection.
            return result;
        }
    }
}
