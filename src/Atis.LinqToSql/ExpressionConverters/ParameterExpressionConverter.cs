using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
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
                            throw new InvalidOperationException($"No SqlExpression found for ParameterExpression '{this.Expression}'. This error usually indicates that the Query Method converter is not converting the first parameter to SqlQueryExpression, e.g. Select(customExpression, x => x.Field1), assume that 'customExpression' is not converting correctly to SqlQueryExpression, therefore, parameter 'x' will not be linked to any SqlQueryExpression instance and will cause this error when translating 'x' part of 'x.Field1' expression. Another reason could be that a custom query method's LambdaExpression parameter is not being mapped to any Data Source. E.g. CustomMethod(query, x => x.Field1, (p1, p2) => new {{ p1, p2 }}), so 'p1' and 'p2' parameters might be presenting data sources but not mapped to any. This is the responsibility of CustomMethod converter class to map those using ILambdaParameterToDataSourceMapper.");
            var isLeafNode = !(this.ParentExpression is MemberExpression);
            // isLeafNode is true when the ParameterExpression is selected alone
            if (!(sqlExpression is SqlQueryExpression || sqlExpression is SqlDataSourceExpression))
                throw new InvalidOperationException($"'{sqlExpression}' is neither SqlQueryExpression nor SqlDataSourceExpression");

            if (sqlExpression is SqlSubQueryExpression subQuery)
            {
                sqlExpression = subQuery.CreateCopy();
                return sqlExpression;
            }

            SqlExpression result;
            if (isLeafNode)
            {
                if (sqlExpression is SqlQueryExpression sqlQuery && !sqlQuery.IsMultiDataSourceQuery)
                {
                    var scalarVal = sqlQuery.GetScalarColumnExpression();
                    if (scalarVal != null)
                        return scalarVal;
                }
                if (sqlExpression is SqlDataSourceExpression ds && ds.NodeType == SqlExpressionType.SubQueryDataSource)
                {
                    var otherDataSourceQuery = ds.QuerySource as SqlQueryExpression
                                                ??
                                                throw new InvalidOperationException($"'{ds.QuerySource}' is not a SqlQueryExpression");
                    // other data source cannot be modified itself, it will always make a copy whenever used
                    var newSqlQuery = otherDataSourceQuery.CreateCopy();
                    result = newSqlQuery;
                }
                else
                {
                    if (sqlExpression is SqlQueryExpression sqlQuery2)
                        result = this.SqlFactory.CreateQueryReference(sqlQuery2);
                    else if (sqlExpression is SqlDataSourceExpression dataSource)
                        result = this.SqlFactory.CreateDataSourceReference(dataSource);
                    else
                        throw new InvalidOperationException($"'{sqlExpression}' is neither SqlQueryExpression nor SqlDataSourceExpression");
                }
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
