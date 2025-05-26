using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating instances of <see cref="NavigationJoinCallExpressionConverter"/>.
    ///     </para>
    /// </summary>
    public class NavigationJoinCallExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<NavigationJoinCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigationJoinCallExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public NavigationJoinCallExpressionConverterFactory(IConversionContext context) : base(context)
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
            if (expression is NavigationJoinCallExpression navigationExpression)
            {
                converter = new NavigationJoinCallExpressionConverter(this.Context, navigationExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts <see cref="NavigationJoinCallExpression"/> instances to SQL expressions.
    ///     </para>
    /// </summary>
    public class NavigationJoinCallExpressionConverter : LinqToSqlQueryConverterBase<NavigationJoinCallExpression>
    {
        private SqlSelectExpression sourceQuery;
        private SqlQueryShapeExpression navigationParent;
        private SqlDataSourceQueryShapeExpression joinedDataSourceQueryShape;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="NavigationJoinCallExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converters">The stack of converters representing the parent chain for context-aware conversion.</param>
        public NavigationJoinCallExpressionConverter(IConversionContext context, NavigationJoinCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters)
            : base(context, expression, converters)
        {
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            // trying to avoid the conversion of SqlSelectExpression to SqlDerivedTableExpression if the navigation
            // has already been added
            if (sourceExpression == this.Expression.JoinedDataSource ||
                sourceExpression == this.Expression.JoinCondition)
            {
                if (this.navigationParent != null && this.joinedDataSourceQueryShape == null && 
                    this.sourceQuery.TryResolveNavigationDataSource(this.navigationParent, this.Expression.NavigationProperty, out _))
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
            // NavigationJoin(db.Table1, t1 => t1, t1 => db.Table2, (t1, t2) => t1.PK == t2.FK, joinType, "NavProp1")

            if (childNode == this.Expression.QuerySource)       // main query converted
            {
                this.sourceQuery = convertedExpression.CastTo<SqlSelectExpression>();

                // IMPORTANT: we need to perform wrapping right here because we don't want the mapping to be invalidated
                this.sourceQuery.WrapIfRequired(SqlQueryOperation.NavigationJoin);

                var arg1Param0 = this.Expression.ParentSelection.Parameters[0];
                this.MapParameter(arg1Param0, () => this.sourceQuery.GetQueryShapeForDataSourceMapping());
            }
            else if (childNode == this.Expression.ParentSelection)
            {
                var arg2Param0 = this.Expression.JoinedDataSource.Parameters[0];
                this.navigationParent = convertedExpression.CastTo<SqlQueryShapeExpression>();
                this.MapParameter(arg2Param0, () => this.navigationParent);
            }
            else if (childNode == this.Expression.JoinedDataSource)     // joined source converted
            {
                if (!this.sourceQuery.TryResolveNavigationDataSource(this.navigationParent, this.Expression.NavigationProperty, out var joinedQueryShape))
                {
                    var joinedDerivedTable = convertedExpression.CastTo<SqlDerivedTableExpression>();
                    var joinedSource = joinedDerivedTable.ConvertToTableIfPossible();
                    this.joinedDataSourceQueryShape = this.sourceQuery.AddNavigationJoin(this.navigationParent, joinedSource, this.Expression.SqlJoinType, this.Expression.NavigationProperty);

                    if (this.Expression.JoinCondition != null)
                    {
                        var arg3 = this.Expression.JoinCondition.ExtractLambdaRequired();
                        var arg3Param0 = arg3.Parameters[0];
                        var arg3Param1 = arg3.Parameters[1];
                        SqlExpression getDataSourceShape() => this.joinedDataSourceQueryShape;
                        SqlExpression getNavigationParent() => this.navigationParent;
                        if (this.Expression.NavigationType == NavigationType.ToParent || this.Expression.NavigationType == NavigationType.ToParentOptional)
                        {
                            this.MapParameter(arg3Param0, getDataSourceShape);
                            this.MapParameter(arg3Param1, getNavigationParent);
                        }
                        else
                        {
                            this.MapParameter(arg3Param0, getNavigationParent);
                            this.MapParameter(arg3Param1, getDataSourceShape);
                        }
                    }
                }
            }
            base.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // convertedChildren[0] = source query
            // convertedChildren[1] = parent selection
            // convertedChildren[2] = joined data source
            // convertedChildren[3] = join condition
            // convertedChildren[4] = join type
            // convertedChildren[5] = navigation property name
            if (this.joinedDataSourceQueryShape != null && this.Expression.JoinCondition != null)
            {
                var joinConditionSqlExpression = convertedChildren[3]; // join condition
                this.sourceQuery.UpdateJoin(this.joinedDataSourceQueryShape.DataSourceAlias, this.Expression.SqlJoinType, joinConditionSqlExpression, joinName: this.Expression.NavigationProperty, navigationJoin: true);
            }
            return this.sourceQuery;
        }

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => this.Expression.QuerySource == childNode;
    }
}
