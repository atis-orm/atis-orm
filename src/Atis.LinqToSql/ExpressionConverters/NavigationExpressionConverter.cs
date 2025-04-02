using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.ExpressionExtensions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating instances of <see cref="NavigationExpressionConverter"/>.
    ///     </para>
    /// </summary>
    public class NavigationExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<NavigationExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigationExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public NavigationExpressionConverterFactory(IConversionContext context) : base(context)
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
            if (expression is NavigationExpression navigationExpression)
            {
                converter = new NavigationExpressionConverter(this.Context, navigationExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts <see cref="NavigationExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class NavigationExpressionConverter : LinqToSqlExpressionConverterBase<NavigationExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterToDataSourceMap;
        private SqlExpression navigationParent;
        private SqlDataSourceExpression joinedDataSource;
        private bool applyJoin;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigationExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converters">The stack of converters representing the parent chain for context-aware conversion.</param>
        public NavigationExpressionConverter(IConversionContext context, NavigationExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters)
            : base(context, expression, converters)
        {
            this.parameterToDataSourceMap = context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            // we are trying to avoid the conversion for the joined data source again because it will just increase
            // the Alias count for no reason, plus the join has already been created
            if (sourceExpression == this.Expression.JoinedDataSource)
            {
                var navigationParentSqlQuery = this.GetNavigationParentSqlQuery();
                if (navigationParentSqlQuery.TryGetNavigationDataSource(this.navigationParent, this.Expression.NavigationProperty, out var ds))
                {
                    convertedExpression = ds;
                    return true;
                }
            }
            return base.TryOverrideChildConversion(sourceExpression, out convertedExpression);
        }

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.SourceExpression)          // source in which join needs to be added is converted
            {
                this.navigationParent = (convertedExpression as SqlDataSourceReferenceExpression)?.DataSource ?? convertedExpression;
            }
            else if (childNode == this.Expression.JoinedDataSource)     // the joined table is converted
            {

                // this.navigationParent can be a SqlDataSourceExpression or can be the SqlQueryExpression.
                // Initially if the navigation property is directly applied to the parameter, for example, x.NavItem
                // then this.navigationParent will be `x` that is the query itself
                // if the nested navigation is used, for example, x.NavItem.NavItemBase
                // in that case first call landed here as x.NavItem which will be translated to a new SqlDataSourceExpression (NavItem_2)
                // and this data source is going to be added in `x` query.
                // Then 2nd time we'll land here for NavItem.NavItemBase, NavItem has already been translated to NavItem_2 SqlDataSourceExpression
                // so NavItemBase will be added as a new SqlDataSourceExpression (NavItemBase_3), in `x` query. You might wonder why this
                // new navigation is being added to `x` query instead of NavItem_2, because NavItem_2 is not really a query, it's simply a Data Source
                // added to `x` and all the new nested navigations will be added to same query as joins

                var navigationParentSqlQuery = this.GetNavigationParentSqlQuery();

                // Note: navigationParentSqlQuery variable is different then this.navigationParent
                // this.navigationParent can be a child data source within the query, navigationParentSqlQuery will
                // be the parent SqlQueryExpression extracted through this.navigationParent

                // in below call, `this.navigationParent` (1st parameter) is important pass because it can be a child Data Source
                // notice the   !  (not)
                //    _________/
                //   /
                if (!navigationParentSqlQuery.TryGetNavigationDataSource(this.navigationParent, this.Expression.NavigationProperty, out this.joinedDataSource))
                {
                    this.applyJoin = true;
                    var sqlQuerySource = convertedExpression as SqlQuerySourceExpression
                                         ?? throw new InvalidOperationException($"Expected a SqlQuerySourceExpression but got {convertedExpression.GetType().Name}");
                    if (this.Expression.SqlJoinType != SqlJoinType.CrossApply && this.Expression.SqlJoinType != SqlJoinType.OuterApply)
                        sqlQuerySource = sqlQuerySource.ConvertToTableIfPossible();
                    this.joinedDataSource = this.SqlFactory.CreateDataSourceForNavigation(sqlQuerySource, this.Expression.NavigationProperty);
                    navigationParentSqlQuery.AddNavigationDataSource(this.navigationParent, this.joinedDataSource, this.Expression.NavigationProperty);
                }

                if (this.Expression.JoinCondition != null)
                {
                    this.MapDataSourceWithLambdaParameter(this.Expression.JoinCondition, this.joinedDataSource);
                }
            }
        }

        private SqlQueryExpression GetNavigationParentSqlQuery()
        {
            var parentDs = this.navigationParent;
            if (parentDs is SqlDataSourceReferenceExpression dsRef)
            {
                parentDs = dsRef.DataSource;
            }
            if (parentDs is SqlQueryExpression queryDataSource)
            {
                return queryDataSource;
            }
            else if (parentDs is SqlDataSourceExpression innerDs)
            {
                return innerDs.ParentSqlQuery;
            }
            else
                throw new InvalidOperationException($"navigationParent is neither SqlQueryExpression nor SqlDataSourceExpression.");
        }

        private void MapDataSourceWithLambdaParameter(Expression expression, SqlExpression dataSource)
        {
            var argParam = this.GetLambdaParameter(expression);
            if (argParam != null)
                this.parameterToDataSourceMap.TrySetParameterMap(argParam, dataSource);
        }

        private ParameterExpression GetLambdaParameter(Expression expression)
        {
            var arg1 = expression;
            if (arg1 is UnaryExpression unaryExpr)
                arg1 = unaryExpr.Operand;
            var arg1Lambda = arg1 as LambdaExpression
                             ?? throw new InvalidOperationException($"LambdaExpression was not extracted from Expression '{expression}'.");
            return arg1Lambda.Parameters[0];
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // convertedChildren[0] is the source expression
            // convertedChildren[1] is the joined data source
            SqlExpression joinConditionSqlExpression = null;
            if (this.Expression.JoinCondition != null)
                joinConditionSqlExpression = convertedChildren[2]; // join condition

            if (applyJoin)
            {
                var joinExpression = this.SqlFactory.CreateJoin(this.Expression.SqlJoinType, joinedDataSource, joinConditionSqlExpression);
                var navigationParentSqlQuery = this.GetNavigationParentSqlQuery();
                navigationParentSqlQuery.ApplyJoin(joinExpression);
            }

            return this.SqlFactory.CreateDataSourceReference(this.joinedDataSource);
        }

        /// <inheritdoc/>
        public override void OnAfterVisit()
        {
            if (this.Expression.JoinCondition != null)
                this.RemoveDataSourceMap(this.Expression.JoinCondition);
        }

        private void RemoveDataSourceMap(Expression expression)
        {
            var argParam = this.GetLambdaParameter(expression);
            this.parameterToDataSourceMap.RemoveParameterMap(argParam);
        }
    }
}
