using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
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

    // IMPORTANT: we are assuming that parent converter will never be MemberExpressionConverter

    /// <summary>
    ///     <para>
    ///         Converts <see cref="NavigationExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class NavigationExpressionConverter : LinqToNonSqlQueryConverterBase<NavigationExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper parameterToDataSourceMap;
        private SqlDataSourceReferenceExpression navigationParent;
        private SqlDataSourceExpression joinedDataSource;
        private bool newNavigationAdded;
        private ParameterExpression lambdaParameterMapped;

        //private ModelPath memberChain;

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

        private ModelPath? _navigationPath;
        private ModelPath NavigationPath
        {
            get
            {
                if (this._navigationPath is null)
                    this._navigationPath = new ModelPath(this.Expression.NavigationProperty);
                return this._navigationPath.Value;
            }
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            // trying to avoid the conversion of SqlSelectExpression to SqlDerivedTableExpression if the navigation
            // has already been added
            if (sourceExpression == this.Expression.JoinedDataSource ||
                sourceExpression == this.Expression.JoinCondition)
            {
                var navigationParentSqlQuery = this.GetNavigationParentSqlQuery();
                if (!this.newNavigationAdded && navigationParentSqlQuery.TryResolveNavigationDataSource(this.navigationParent, this.NavigationPath, out _))
                {
                    convertedExpression = this.SqlFactory.CreateLiteral("dummy");
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
                // it should always be a reference
                this.navigationParent = convertedExpression as SqlDataSourceReferenceExpression 
                                        ??
                                        throw new InvalidOperationException($"Expected a {nameof(SqlDataSourceReferenceExpression)} but got {convertedExpression.GetType().Name}, childNode = '{childNode}'.");
            }
            else if (childNode == this.Expression.JoinedDataSource)     // the joined table is converted
            {
                var navigationParentSqlQuery = this.GetNavigationParentSqlQuery();

                if (!navigationParentSqlQuery.TryResolveNavigationDataSource(this.navigationParent, this.NavigationPath, out this.joinedDataSource))
                {
                    this.joinedDataSource = navigationParentSqlQuery.AddNavigationJoin(navigationParent, convertedExpression, this.Expression.SqlJoinType, this.NavigationPath, this.Expression.NavigationProperty);
                    this.newNavigationAdded = true;
                }             

                if (this.Expression.JoinCondition != null)
                {
                    this.lambdaParameterMapped = this.GetLambdaParameter(this.Expression.JoinCondition);
                    this.parameterToDataSourceMap.TrySetParameterMap(this.lambdaParameterMapped, this.joinedDataSource);
                }
            }
        }

        private SqlSelectExpression GetNavigationParentSqlQuery()
        {
            var parentDs = this.navigationParent;
            if (parentDs is SqlDataSourceExpression dsRef)
            {
                parentDs = dsRef.SelectQuery;
            }
            if (parentDs is SqlSelectExpression queryDataSource)
            {
                return queryDataSource;
            }
            else
                throw new InvalidOperationException($"navigationParent is neither {nameof(SqlSelectExpression)} nor {nameof(SqlDataSourceExpression)}.");
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

            if (this.joinedDataSource is null)
                throw new InvalidOperationException($"{nameof(this.joinedDataSource)} is null, this should not happen.");

            if (this.newNavigationAdded && this.Expression.JoinCondition != null)
            {
                var joinConditionSqlExpression = convertedChildren[2]; // join condition
                var parentSelectQuery = this.GetNavigationParentSqlQuery();
                var navigationParentAlias = (this.navigationParent as SqlDataSourceExpression)?.DataSourceAlias;
                parentSelectQuery.UpdateJoin(this.joinedDataSource.DataSourceAlias, this.Expression.SqlJoinType, joinConditionSqlExpression, joinName: this.Expression.NavigationProperty, navigationJoin: true, navigationParent: navigationParentAlias);
            }

            return this.joinedDataSource
                    ??
                    throw new InvalidOperationException($"joinedDataSource is not set");
        }

        /// <inheritdoc/>
        public override void OnAfterVisit()
        {
            if (this.lambdaParameterMapped != null)
                this.parameterToDataSourceMap.RemoveParameterMap(this.lambdaParameterMapped);
        }
    }
}
