using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating instances of <see cref="ChildJoinExpressionConverter"/>.
    ///     </para>
    /// </summary>
    public class ChildJoinExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<ChildJoinExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ChildJoinExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public ChildJoinExpressionConverterFactory(IConversionContext context) : base(context)
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
        /// <returns>
        ///     <c>true</c> if a suitable converter was successfully created; otherwise, <c>false</c>.
        /// </returns>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is ChildJoinExpression childJoinExpression)
            {
                converter = new ChildJoinExpressionConverter(this.Context, childJoinExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for transforming <see cref="ChildJoinExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class ChildJoinExpressionConverter : LinqToSqlExpressionConverterBase<ChildJoinExpression>
    {
        private SqlQueryExpression sourceSqlQuery;
        private SqlDataSourceExpression joinedDataSource;
        private ParameterExpression joinLambdaExpressionParameter;

        private bool defaultIfEmpty;
        private readonly ILambdaParameterToDataSourceMapper parameterMap;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ChildJoinExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ChildJoinExpressionConverter(IConversionContext context, ChildJoinExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.parameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childPlugin, Expression childNode, SqlExpression convertedExpression)
        {
            if (this.Expression.Parent == childNode)          // parent source
            {
                try
                {
                    this.sourceSqlQuery = (convertedExpression as SqlDataSourceReferenceExpression)?.DataSource as SqlQueryExpression
                                                ??
                                            ((convertedExpression as SqlDataSourceReferenceExpression)?.DataSource as SqlDataSourceExpression)?.ParentSqlQuery
                                                ??
                                            (convertedExpression as SqlSelectedCollectionExpression)?.SourceExpression as SqlQueryExpression
                                                ??
                                            ((convertedExpression as SqlSelectedCollectionExpression)?.SourceExpression as SqlDataSourceExpression)?.ParentSqlQuery
                                                ??
                                            convertedExpression as SqlQueryExpression
                                                ??
                                            throw new InvalidOperationException($"Parent was not converted to {nameof(SqlQueryExpression)}");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ChildJoinExpression Converter: System is unable to extract the source sql query, make sure if the expression is a SqlDataSourceExpression then it's ParentSqlQuery is set, this can happen if a SqlDataSourceExpression was created but never added to SqlQueryExpression. See inner exception for details.", ex);
                }
            }
            else if (this.Expression.Query == childNode)     // child source
            {
                var childSqlQuery = convertedExpression as SqlQueryExpression
                                    ??
                                    throw new InvalidOperationException($"Property {nameof(ChildJoinExpression)}.{nameof(ChildJoinExpression.Query)} (Child Query) was not converted to {nameof(SqlQueryExpression)}");
                this.defaultIfEmpty = childSqlQuery.IsDefaultIfEmpty;
                SqlQuerySourceExpression joinedSource;
                if (childSqlQuery.IsTableOnly())
                {
                    joinedSource = childSqlQuery.InitialDataSource.QuerySource;      // SqlTableExpression
                }
                else
                {
                    joinedSource = childSqlQuery;                                   // SqlQueryExpression
                }

                // You might think why not put IsAutoAddedNavigationDataSource = true while creating below Data Source
                // but we don't have to, since this ChildJoinExpression is a very special case used only in SelectMany
                // if we set it to true, then it might make problems in extracting the data sources during projection,
                // because later this data source is marked as DefaultDataSource in SqlQueryExpression, so it might become
                // conflicting that same data source marked as Auto Added and then becoming Default Data Source.

                this.joinedDataSource = this.SqlFactory.CreateDataSourceForNavigation(joinedSource, this.Expression.NavigationName);
                this.sourceSqlQuery.AddDataSource(joinedDataSource);

                if (this.Expression.JoinCondition != null)
                {
                    this.joinLambdaExpressionParameter = GetJoinLambdaExpressionParameter();
                    this.parameterMap.TrySetParameterMap(joinLambdaExpressionParameter, this.joinedDataSource);
                }
            }
        }

        private ParameterExpression GetJoinLambdaExpressionParameter()
        {
            var joinExpression = this.Expression.JoinCondition
                                    ??
                                    throw new InvalidOperationException($"Join Condition was not given.");
            var firstParam = joinExpression.Parameters.First();
            return firstParam;
        }

        /// <inheritdoc />
        public override void OnAfterVisit()
        {
            if (this.joinLambdaExpressionParameter != null)
            {
                this.parameterMap.RemoveParameterMap(this.joinLambdaExpressionParameter);
            }
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // convertedChildren[0] = parent
            // convertedChildren[1] = child source
            var joinPredicate = convertedChildren[2];        // join predicate

            SqlJoinType sqlJoinType = SqlJoinType.Inner;
            if (this.defaultIfEmpty)
                sqlJoinType = SqlJoinType.Left;
            var joinExpression = this.SqlFactory.CreateJoin(sqlJoinType, this.joinedDataSource, joinPredicate);
            this.sourceSqlQuery.ApplyJoin(joinExpression);

            return this.sourceSqlQuery;
        }
    }
}
