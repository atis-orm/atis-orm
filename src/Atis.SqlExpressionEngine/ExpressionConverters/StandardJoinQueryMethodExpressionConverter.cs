using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
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
        private SqlExpression sourceColumnSelection;
        private SqlBinaryExpression joinCondition;

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

        private bool IsGroupJoin => this.Expression.Method.Name == nameof(Queryable.GroupJoin);
        // SelectMany will flatten the result, so we'll consider the GroupJoin as normal Join
        private bool UseSubQueryDataSource => IsGroupJoin && !(this.ConverterStack.FirstOrDefault() is SelectManyQueryMethodExpressionConverter);

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
                if (!this.UseSubQueryDataSource && otherDataSqlQuery.IsTableOnly())
                {
                    querySource = otherDataSqlQuery.InitialDataSource.QuerySource;
                }
                else
                {
                    if (this.UseSubQueryDataSource)
                        querySource = this.SqlFactory.CreateSubQuery(otherDataSqlQuery);
                    else
                        querySource = otherDataSqlQuery;
                }

                if (this.UseSubQueryDataSource)
                {
                    this.joinedDataSource = this.SqlFactory.CreateDataSourceForSubQuery(Guid.NewGuid(), querySource);
                    this.SourceQuery.AddSubQueryDataSource(this.joinedDataSource);
                }
                else
                {
                    this.joinedDataSource = this.SqlFactory.CreateDataSourceForJoinedSource(Guid.NewGuid(), querySource);
                    this.SourceQuery.AddDataSource(this.joinedDataSource);
                }

                var otherColumnLambda = this.GetArgumentLambda(this.OtherColumnsArgIndex);
                var selectLambda = this.GetArgumentLambda(this.SelectArgIndex);
                if (this.UseSubQueryDataSource)
                {
                    this.ParameterMap.TrySetParameterMap(otherColumnLambda.Parameters[0], querySource);
                }

                else
                {
                    this.ParameterMap.TrySetParameterMap(otherColumnLambda.Parameters[0], this.joinedDataSource);
                }

                if (this.UseSubQueryDataSource)
                {
                    this.ParameterMap.RemoveParameterMap(selectLambda.Parameters[0]);
                    this.ParameterMap.TrySetParameterMap(selectLambda.Parameters[0], this.SourceQuery);
                    this.ParameterMap.TrySetParameterMap(selectLambda.Parameters[1], querySource);
                }
                else
                {
                    // map 2nd argument of select argument to other data source
                    this.ParameterMap.TrySetParameterMap(selectLambda.Parameters[1], this.joinedDataSource);
                }
            }
            else if (argIndex == this.SourceColumnsArgIndex)
            {
                this.sourceColumnSelection = converterArgument;
            }
            else if (argIndex == this.OtherColumnsArgIndex)
            {
                // here we'll know that both parameters are converted

                var otherColumnSelection = converterArgument;

                SqlBinaryExpression joinPredicate = null;

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
                            var condition = this.SqlFactory.CreateBinary(sourceColumn, otherColumn, SqlExpressionType.Equal);
                            joinPredicate = joinPredicate == null ? condition : this.SqlFactory.CreateBinary(joinPredicate, condition, SqlExpressionType.AndAlso);
                        }
                    }
                    else
                        throw new InvalidOperationException($"Expected {nameof(SqlCollectionExpression)} for other columns selection Arg-Index: {this.OtherColumnsArgIndex}.");
                }
                else
                {
                    joinPredicate = this.SqlFactory.CreateBinary(sourceColumnSelection, otherColumnSelection, SqlExpressionType.Equal);
                }

                if (this.UseSubQueryDataSource)
                {
                    (this.joinedDataSource.QuerySource as SqlQueryExpression)?.ApplyWhere(joinPredicate);
                }

                this.joinCondition = joinPredicate;
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            var projection = arguments[3];

            if (!this.UseSubQueryDataSource)
            {
                var joinType = joinedDataSource.GetJoinType() == SqlJoinType.Left ? SqlJoinType.Left : SqlJoinType.Inner;
                var joinExpression = this.SqlFactory.CreateJoin(joinType, joinedDataSource, joinCondition);
                sqlQuery.ApplyJoin(joinExpression);
            }

            if (this.HasProjection)
            {
                if (this.HasDefaultProjection())
                {
                    this.UpdateModelMaps(sqlQuery.AllQuerySources);
                }
                else
                {
                    sqlQuery.ApplyProjection(projection);
                }
            }

            return sqlQuery;
        }

        private void UpdateModelMaps(IReadOnlyCollection<SqlDataSourceExpression> allDataSources)
        {
            var extractor = new DataSourcePropertyInfoExtractor();
            var updatedMap = extractor.RecalculateMemberMapping(GetArgumentLambda(SelectArgIndex));
            var lastDs = allDataSources.Last();

            if (updatedMap.CurrentDataSourceMemberInfo != null)
            {
                foreach (var ds in allDataSources)
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


        private bool HasProjection => this.Expression.Arguments.Count >= 5;

        private bool HasDefaultProjection()
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
