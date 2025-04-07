using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.Internal;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
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
        private readonly int selectArgIndex = 2;
        private bool newDataSourceAdded;

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

        /// <summary>
        ///     <para>
        ///         Determines if any outside data source has been used in the specified SQL query.
        ///     </para>
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <returns><c>true</c> if an outside data source has been used; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     <para>
        ///         This method is used to determine whether the join should be cross join or outer apply.
        ///         If there is an outside data source has been used in the new query, then it should be outer apply; otherwise, cross join.
        ///     </para>
        /// </remarks>
        protected virtual bool HasOutsideDataSourceBeenUsed(SqlQueryExpression sqlQuery)
        {
            var otherDataSourceUsed = new AnyOtherDataSourceReferenceUsed();
            return otherDataSourceUsed.HasOutsideDataSourceBeenUsed(sqlQuery);
        }

        /// <inheritdoc />
        protected override void OnArgumentConverted(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression argument, SqlExpression convertedArgument)
        {
            var argIndex = this.Expression.Arguments.IndexOf(argument);
            if (argIndex == 1)
            {
                if (this.SourceQuery != convertedArgument)      // otherQuery is NOT a navigation property
                {
                    // if we are here it means source query was not used in SelectMany,
                    // e.g. x.SelectMany(x => db.OtherTable)
                    // here 'x' is the parameter in SelectMany which is the source query, but was not used in the start
                    // rather another table was selected from db.
                    // This will cause convertedArgument to be a different query then this.SourceQuery
                    var otherSqlExpr = convertedArgument;

                    if (otherSqlExpr is SqlDataSourceReferenceExpression dsRef)
                        otherSqlExpr = dsRef.DataSource;

                    if (otherSqlExpr is SqlQueryExpression otherSqlQuery)
                    {
                        SqlQuerySourceExpression newQuerySource;
                        //string dataSourceAlias;
                        bool crossJoin;
                        if (otherSqlQuery.IsTableOnly())
                        {
                            newQuerySource = otherSqlQuery.InitialDataSource.QuerySource;
                            //dataSourceAlias = otherSqlQuery.InitialDataSource.DataSourceAlias;
                            crossJoin = true;
                        }
                        else
                        {
                            // Right now we are saying if outside data sources have been used in the new query
                            // then we cannot make it cross join and this is the only condition. However, there
                            // might be scenario where we need to make it cross apply because of some other reasons
                            // for example, top with order by which may require cross apply instead of cross join.
                            // Although, top with order by not really requires cross apply, it works with cross join
                            // but it's just an example.
                            if (this.HasOutsideDataSourceBeenUsed(otherSqlQuery))
                                crossJoin = false;      // this will be outer apply
                            else
                                crossJoin = true;       // this will be cross join
                            newQuerySource = otherSqlQuery;
                            //dataSourceAlias = this.Context.GenerateAlias();
                        }

                        var newDataSource = this.SqlFactory.CreateDataSourceForQuerySource(newQuerySource);
                        if (crossJoin)
                        {
                            this.SourceQuery.AddDataSource(newDataSource);
                        }
                        else
                        {
                            // cross apply or outer apply
                            SqlJoinType sqlJoinType = SqlJoinType.CrossApply;
                            if (otherSqlQuery.IsDefaultIfEmpty)
                                sqlJoinType = SqlJoinType.OuterApply;
                            var crossApplyJoin = this.SqlFactory.CreateJoin(sqlJoinType, newDataSource, joinPredicate: null);
                            this.SourceQuery.ApplyJoin(crossApplyJoin);
                        }

                        this.newDataSourceAdded = true;
                    }
                    else
                    {
                        // TODO: we are not yet encountered a scenario where the converted expression is other than
                        //          SqlQueryExpression. However, we have the case where convertedArgument is a
                        //          SqlDataSourceReferenceExpression, but in that case that sql data source
                        //          has already been added to sql query, so we don't have to do anything
                        //          about it. But still, there might be scenario where that newly created data source
                        //          needs to be added in the sql query.

                        /*
                         * 
                            var q = from e in employees
                                    join ed in employeeDegrees on new { e.RowId, e.EmployeeId } equals new { ed.RowId, ed.EmployeeId }
                                    into eds from ed2 in eds.DefaultIfEmpty()

                        In above case, the last part 'into eds from ed2 in eds.DefaultIfEmpty()' will be SelectMany method call
                        like this, SelectMany(transparentIdentifier2 => transparentIdentifier2.eds.DefaultIfEmpty(), (transparentIdentifier2, ed2) => new { transparentIdentifier2, ed2 })

                        So, we'll land here after we converted 1st arg 'transparentIdentifier2 => transparentIdentifier2.eds.DefaultIfEmpty()' and this will be converted to
                        SqlDataSourceReferenceExpression, which is already part of the SourceQuery.
                        
                        Below we are checking if the SqlDataSourceExpression's DataSource is SqlQuerySourceExpression, and it's IsDefaultIfEmpty is true then we'll find
                        the join in the source query for that data source and make it Left Join.
                         * 
                         */

                        if (otherSqlExpr is SqlDataSourceExpression otherDs &&
                                otherDs.QuerySource.IsDefaultIfEmpty)
                        {
                            var join = this.SourceQuery.Joins.Where(x => x.JoinedSource == otherDs).FirstOrDefault();
                            if (join != null)
                            {
                                if (join.JoinType == SqlJoinType.Inner)
                                    join.UpdateJoinType(SqlJoinType.Left);
                                else if (join.JoinType == SqlJoinType.CrossApply)
                                    join.UpdateJoinType(SqlJoinType.OuterApply);
                            }
                        }
                    }
                }
                else
                {
                    if (this.SourceQuery.Projection != null && this.SourceQuery.AutoProjectionApplied)
                    {
                        this.SourceQuery.ClearAutoProjection();
                    }
                    // if we are here then it means in SelectMany source query was directly used e.g x.SelectMany(x => x.NavChild)
                    if (this.SourceQuery.IsDefaultIfEmpty)
                    {
                        var lastDataSource = this.SourceQuery.DataSources.Last();
                        var join = this.SourceQuery.Joins.Where(x => x.JoinedSource == lastDataSource).First();
                        join.UpdateJoinType(SqlJoinType.Left);

                        // here we are reversing the SourceQuery IsDefaultIfEmpty flag because
                        // we cannot mark the whole query as DefaultIfEmpty, because it will cause
                        // all the new joins to be added as Left Join, which is not the desired behavior
                        this.SourceQuery.IsDefaultIfEmpty = false;
                    }
                }

                if (this.HasProjectionArgument)        // if projection is given
                {
                    // we are not setting Projection Lambda's 1st Parameter, because it's already set
                    // in the base class

                    // either it was added by Inner Join or was added in above block
                    SqlExpression projectionLambdaParam02 = this.SourceQuery.DataSources.Last();

                    var projectionLambda = this.GetArgumentLambda(this.selectArgIndex);

                    this.parameterMap.TrySetParameterMap(projectionLambda.Parameters[1], projectionLambdaParam02);
                }
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            var otherSqlExpr = arguments.First();
            if (this.SourceQuery != otherSqlExpr)
            {
                // arguments Length = 1 means no projection (note that 1st argument is passed as SqlQuery that's why we have less arguments now)
                if (!this.HasDefaultProjection())
                {
                    SqlExpression projection;
                    if (!this.HasProjectionArgument)      // no projection
                    {
                        // pick-up the last data source that was added to the query and apply projection
                        var lastDataSource = sqlQuery.DataSources.Last();
                        projection = this.SqlFactory.CreateDataSourceReference(lastDataSource);
                    }
                    else
                    {
                        // else it means projection parameter was also given in SelectMany, so we'll apply that projection
                        projection = arguments[1];
                        //projection = RemoveDuplicateProjection(otherSqlExpr, projection);
                    }
                    this.SourceQuery.ApplyProjection(projection);
                }
            }
            else
            {
                // arguments Length = 1 means no projection (note that 1st argument is passed as SqlQuery that's why we have less arguments now)
                if (!this.HasProjectionArgument)      // no projection
                {
                    // If we are here it means, the other data source was the navigation property added
                    // to main data source as join.
                    // Usually, SelectMany translates as InnerJoin, but in-case of Child Navigation Collection,
                    // the Navigation Collection is added to sql query as join, that's why instead of getting
                    // new SqlQueryExpression we receive the same SqlQueryExpression source SqlQueryExpression.
                    this.SourceQuery.SetLastAsDefaultDataSource();
                }
                else
                {
                    if (!this.HasDefaultProjection())
                    {
                        // else it means projection parameter was also given in SelectMany, so we'll apply that projection
                        var projection = arguments[1];
                        sqlQuery.ApplyProjection(projection);
                    }
                }
            }

            // arguments = SqlExpressions (they are not method arguments)
            // we are passing 1st argument in sqlQuery parameter, that's why arguments[] array will have 1 item less
            if (this.HasProjectionArgument)       // projection was given
            {
                // it means Selector argument was provided in SelectMany
                //                  0         1           2
                // e.g. SelectMany(source, otherSource, (s, o) => new { s, o }  <-  selector argument)
                var dataSourcePropertyInfoExtractor = new DataSourcePropertyInfoExtractor();
                var updatedMap = dataSourcePropertyInfoExtractor.RecalculateMemberMapping(this.GetArgumentLambda(this.selectArgIndex));
                var lastDs = sqlQuery.DataSources.Last();
                if (updatedMap.CurrentDataSourceMemberInfo != null)
                {
                    foreach (var ds in this.SourceQuery.AllQuerySources)
                    {
                        if (ds != lastDs)
                        {
                            ds.AddModelPathPrefix(updatedMap.CurrentDataSourceMemberInfo.Name);
                        }
                    }
                }
                if (updatedMap.NewDataSourceMemberInfo != null)
                {
                    if (this.newDataSourceAdded || lastDs.ModelPath.IsEmpty)
                        lastDs.AddModelPathPrefix(updatedMap.NewDataSourceMemberInfo.Name);
                    else
                        lastDs.ReplaceModelPathPrefix(updatedMap.NewDataSourceMemberInfo.Name);
                }
            }

            return sqlQuery;
        }

        /// <summary>
        ///     <para>
        ///         Determines whether the expression has a default projection.
        ///     </para>
        /// </summary>
        /// <returns><c>true</c> if the expression has a default projection; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     <para>
        ///         If the projection argument is given in the SelectMany method, then it's not a default projection.
        ///     </para>
        ///     <para>
        ///         Default projection is usually both parameters are selected in the NewExpression.
        ///     </para>
        /// </remarks>
        protected virtual bool HasDefaultProjection()
        {
            if (this.HasProjectionArgument)
            {
                var selectLambda = this.GetArgumentLambda(this.selectArgIndex);
                var param0 = selectLambda.Parameters[0];
                var param1 = selectLambda.Parameters[1];
                var lambdaBody = selectLambda.Body;
                if (lambdaBody is NewExpression newExpr &&
                    newExpr.Arguments.Count == 2 &&
                    newExpr.Arguments[0] == param0 &&
                    newExpr.Arguments[1] == param1)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Visitor class to check if any other data source reference has been used.
        /// </summary>
        class AnyOtherDataSourceReferenceUsed : SqlExpressionVisitor
        {
            private SqlQueryExpression sqlQuery;
            private bool otherDataSourceHasBeenUsed;

            /// <summary>
            /// Determines whether an outside data source has been used in the specified SQL query.
            /// </summary>
            /// <param name="sqlQuery">The SQL query to check.</param>
            /// <returns><c>true</c> if an outside data source has been used; otherwise, <c>false</c>.</returns>
            public bool HasOutsideDataSourceBeenUsed(SqlQueryExpression sqlQuery)
            {
                this.sqlQuery = sqlQuery;
                this.otherDataSourceHasBeenUsed = false;
                this.Visit(sqlQuery);
                return this.otherDataSourceHasBeenUsed;
            }

            /// <summary>
            /// Checks if the specified alias is part of the current SQL query's data sources.
            /// </summary>
            /// <param name="alias">The alias to check.</param>
            private void IsMyDataSource(Guid alias)
            {
                if (otherDataSourceHasBeenUsed)
                    return;

                var isMyDataSource = this.sqlQuery.DataSources.Any(x => x.DataSourceAlias == alias);
                if (!isMyDataSource)
                    this.otherDataSourceHasBeenUsed = true;
            }

            /// <inheritdoc />
            public override SqlExpression Visit(SqlExpression node)
            {
                if (this.otherDataSourceHasBeenUsed)
                    return node;
                return base.Visit(node);
            }

            /// <inheritdoc />
            protected internal override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
            {
                this.IsMyDataSource(sqlDataSourceColumnExpression.DataSource.DataSourceAlias);
                return sqlDataSourceColumnExpression;
            }
        }
    }
}
