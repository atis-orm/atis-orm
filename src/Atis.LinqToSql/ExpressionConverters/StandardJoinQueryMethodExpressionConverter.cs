using Atis.Expressions;
using Atis.LinqToSql.Internal;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating LINQ's Join method.
    ///     </para>
    /// </summary>
    public class StandardJoinQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="StandardJoinQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public StandardJoinQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.Name == nameof(Queryable.Join) ||
                    methodCallExpression.Method.Name == nameof(Queryable.GroupJoin);
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new StandardJoinQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for standard join query methods.
    ///     </para>
    /// </summary>
    public class StandardJoinQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        private SqlDataSourceExpression joinedDataSource;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="StandardJoinQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public StandardJoinQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /*


          Join(
             0: this IQueryable<T> source,
             1: IQueryable<R> otherData, 
             2: source => source.PK / source => new { source.PK1, source.PK2 }, 
             3: otherData => otherData.FK / otherData => new { otherData.FK1, otherData.FK2 }, 
             4: (source, otherData) => new { source.Field1, source.Field2, otherData.Field1, otherData.Field2 }
            )

        */

        private int OtherDataArgIndex => 1;
        private int SourceColumnsArgIndex => 2;
        private int OtherColumnsArgIndex => 3;
        private int SelectArgIndex => 4;

        /// <inheritdoc />
        protected override void OnSourceQueryCreated()
        {
            // base.OnSourceQueryCreated will be mapping the lambda parameter, so we want to override that behavior
            if (this.SourceQuery is null)
                throw new InvalidOperationException($"this.SourceQuery is null.");

            var sourceColumnLambda = this.GetArgumentLambda(this.SourceColumnsArgIndex);
            var selectLambda = this.GetArgumentLambda(this.SelectArgIndex);

            this.ParameterMap.TrySetParameterMap(sourceColumnLambda.Parameters[0], this.SourceQuery);
            this.ParameterMap.TrySetParameterMap(selectLambda.Parameters[0], this.SourceQuery.InitialDataSource);
        }

        /// <inheritdoc />
        protected override void OnArgumentConverted(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression argument, SqlExpression converterArgument)
        {
            var argIndex = this.Expression.Arguments.IndexOf(argument);
            if (argIndex == this.OtherDataArgIndex)     // this is the other data source converted
            {
                if (converterArgument is SqlDataSourceReferenceExpression dsRef)
                    converterArgument = dsRef.DataSource;
                var otherDataSqlQuery = converterArgument as SqlQueryExpression
                                           ??
                                           throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} on the stack");

                SqlQuerySourceExpression querySource;
                //string alias;
                if (otherDataSqlQuery.IsTableOnly())
                {
                    querySource = otherDataSqlQuery.InitialDataSource.DataSource;
                    //alias = otherDataSqlQuery.InitialDataSource.DataSourceAlias;
                }
                else
                {
                    querySource = otherDataSqlQuery;
                    //alias = this.Context.GenerateAlias();
                }

                this.joinedDataSource = new SqlDataSourceExpression(querySource);
                this.SourceQuery.AddDataSource(this.joinedDataSource);

                var otherColumnLambda = this.GetArgumentLambda(this.OtherColumnsArgIndex);
                var selectLambda = this.GetArgumentLambda(this.SelectArgIndex);
                this.ParameterMap.TrySetParameterMap(otherColumnLambda.Parameters[0], this.joinedDataSource);

                // map 2nd argument of select argument to other data source
                this.ParameterMap.TrySetParameterMap(selectLambda.Parameters[1], this.joinedDataSource);
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            var sourceColumnSelection = arguments[1];
            var otherColumnSelection = arguments[2];
            var projection = arguments[3];

            SqlExpression joinCondition = null;

            if (sourceColumnSelection is SqlCollectionExpression sourceColumnCollection)
            {
                if (otherColumnSelection is SqlCollectionExpression otherColumnCollection)
                {
                    var sourceColumns = sourceColumnCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
                    var otherColumns = otherColumnCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
                    if (sourceColumns.Length != otherColumns.Length)
                        throw new InvalidOperationException($"Source columns count {sourceColumns.Length} does not match other columns count {otherColumns.Length}.");

                    for (var i = 0; i < sourceColumns.Length; i++)
                    {
                        var sourceColumn = sourceColumns[i].ColumnExpression;
                        var otherColumn = otherColumns[i].ColumnExpression;
                        var condition = new SqlBinaryExpression(sourceColumn, otherColumn, SqlExpressionType.Equal);
                        joinCondition = joinCondition == null ? condition : new SqlBinaryExpression(joinCondition, condition, SqlExpressionType.AndAlso);
                    }
                }
                else
                    throw new InvalidOperationException($"Expected {nameof(SqlCollectionExpression)} for other columns selection Arg-Index: {this.OtherColumnsArgIndex}.");
            }
            else
            {
                joinCondition = new SqlBinaryExpression(sourceColumnSelection, otherColumnSelection, SqlExpressionType.Equal);
            }

            var joinType = this.joinedDataSource.GetJoinType() == SqlJoinType.Left ? SqlJoinType.Left : SqlJoinType.Inner;
            var joinExpression = new SqlJoinExpression(joinType, this.joinedDataSource, joinCondition);
            this.SourceQuery.ApplyJoin(joinExpression);

            if (this.HasProjection)
            {
                if (this.HasAutoProjection())
                {
                    var dataSourcePropertyInfoExtractor = new DataSourcePropertyInfoExtractor();
                    var updatedMap = dataSourcePropertyInfoExtractor.RecalculateMemberMapping(this.GetArgumentLambda(this.SelectArgIndex));
                    var lastDs = sqlQuery.DataSources.Last();
                    if (updatedMap.CurrentDataSourceMemberInfo != null)
                    {
                        foreach (var ds in this.SourceQuery.AllDataSources)
                        {
                            if (ds != lastDs)
                            {
                                ds.AddModelPathPrefix(updatedMap.CurrentDataSourceMemberInfo.Name);
                            }
                        }
                    }
                    if (updatedMap.NewDataSourceMemberInfo != null)
                    {
                        lastDs.AddModelPathPrefix(updatedMap.NewDataSourceMemberInfo.Name);
                    }
                }
                else
                {
                    this.SourceQuery.ApplyProjection(projection);
                }
            }

            return sqlQuery;
        }

        private bool HasProjection => this.Expression.Arguments.Count >= 5;

        private bool HasAutoProjection()
        {
            if (this.HasProjection)
            {
                var selectLambda = this.GetArgumentLambda(this.SelectArgIndex);
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
    }
}
