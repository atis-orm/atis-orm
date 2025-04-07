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
    ///         Factory class for creating converters for explicit join query methods defined in <see cref="QueryExtensions"/>.
    ///     </para>
    /// </summary>
    public class JoinQueryMethodExpressionConverterFactory : QueryMethodExpressionConverterFactoryBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinQueryMethodExpressionConverterFactory"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public JoinQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override bool IsQueryMethodCall(MethodCallExpression methodCallExpression)
        {
            var joinMethods = new string[] { nameof(QueryExtensions.LeftJoin), nameof(QueryExtensions.RightJoin), nameof(QueryExtensions.InnerJoin), nameof(QueryExtensions.CrossApply), nameof(QueryExtensions.OuterApply), nameof(QueryExtensions.FullOuterJoin) };
            return joinMethods.Contains(methodCallExpression.Method.Name)
                    &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions);
        }

        /// <inheritdoc />
        protected override ExpressionConverterBase<Expression, SqlExpression> CreateConverter(MethodCallExpression methodCallExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
        {
            return new JoinQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for explicit join query method (defined in <see cref="QueryExtensions"/>) expression.
    ///     </para>
    /// </summary>
    public class JoinQueryMethodExpressionConverter : QueryMethodExpressionConverterBase
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="JoinQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public JoinQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            // we don't want to the conversion system to go into 'NewExpression' part of Join Query Method
            // therefore, we are returning a dummy expression
            var newExpressionIndex = GetNewExpressionIndex();
            if (newExpressionIndex >= 0)
            {
                if (sourceExpression == this.Expression.Arguments[newExpressionIndex])
                {
                    convertedExpression = this.SqlFactory.CreateLiteral("dummy");
                    return true;
                }
            }
            convertedExpression = null;
            return false;
        }

        private bool IsCrossOrOuterApply()
        {
            return this.Expression.Method.Name == nameof(QueryExtensions.CrossApply) ||
                    this.Expression.Method.Name == nameof(QueryExtensions.OuterApply);
        }

        private int ArgCount => this.Expression.Arguments.Count;
        
        private int GetNewlyJoinedDataSourceIndex()
        {
            if (this.ArgCount == 4)
                return 1;
            else if (this.ArgCount == 3 && this.IsCrossOrOuterApply())
                return 1;
            return -1;
        }
        
        private int GetAlreadyAvailableDataSourceIndex()
        {
            if (this.ArgCount == 3 && !this.IsCrossOrOuterApply())
                return 1;
            return -1;
        }
        
        private int GetAlreadyAvailableJoinedDataSourceIndex()
        {
            if (this.ArgCount == 3)
                return 1;
            return -1;
        }
        
        private int GetNewExpressionIndex()
        {
            if (this.ArgCount == 4)
                return 2;
            else if (this.ArgCount == 3 && this.IsCrossOrOuterApply())
                return 2;
            return -1;
        }
        
        private int GetJoinConditionIndex()
        {
            if (this.ArgCount == 4)
                return 3;
            if (this.ArgCount == 3 && !this.IsCrossOrOuterApply())
                return 2;
            return -1;
        }

        private SqlDataSourceExpression newJoinedDataSource = null;

        /// <inheritdoc />
        protected override void OnArgumentConverted(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression argument, SqlExpression convertedArgument)
        {
            var argIndex = this.Expression.Arguments.IndexOf(argument);
            if (this.GetNewlyJoinedDataSourceIndex() == argIndex)
            {
                if (convertedArgument is SqlDataSourceReferenceExpression dsRef)
                    convertedArgument = dsRef.DataSource;
                var joinedQuery = convertedArgument as SqlQuerySourceExpression
                                        ?? throw new InvalidOperationException($"2nd argument of Join Query Method must be a SqlQuerySourceExpression.");

                //string alias;
                if (joinedQuery is SqlQueryExpression sqlQuery && sqlQuery.IsTableOnly())
                {
                    var firstDataSource = sqlQuery.DataSources.First();
                    joinedQuery = firstDataSource.QuerySource;
                    //alias = firstDataSource.DataSourceAlias;
                }
                else
                {
                    //alias = this.Context.GenerateAlias();
                }
                this.newJoinedDataSource = this.SqlFactory.CreateDataSourceForQuerySource(joinedQuery);
                this.SourceQuery.AddDataSource(this.newJoinedDataSource);
            }
            else if (this.GetNewExpressionIndex() == argIndex)
            {
                var dataSourcePropertyInfoExtractor = new DataSourcePropertyInfoExtractor();
                var updatedMapping = dataSourcePropertyInfoExtractor.RecalculateMemberMapping(this.GetArgumentLambda(argIndex));
                if (updatedMapping.NewDataSourceMemberInfo == null)
                    throw new InvalidOperationException($"Unable to find the new data source in the 2nd argument of Join Query Method, make sure you have selected the 2nd parameter in New Data Source Expression.");

                if (updatedMapping.CurrentDataSourceMemberInfo == null)
                {
                    var dataSourceWithModelPath = this.SourceQuery.AllQuerySources
                                                                    .Where(x => !x.ModelPath.IsEmpty)
                                                                    .Select(x => new { Ds = x, DsModelPath = x.ModelPath.GetLastElement() })
                                                                    .ToDictionary(x => x.DsModelPath, x => x.Ds);
                    foreach (var kv in updatedMapping.NewMap)
                    {
                        if (dataSourceWithModelPath.TryGetValue(kv.Value.Name, out var ds))
                        {
                            ds.ReplaceModelPathPrefix(kv.Key.Name);
                        }
                    }
                }
                else
                {
                    foreach (var ds in this.SourceQuery.AllQuerySources)
                    {
                        if (ds != this.newJoinedDataSource)
                        {
                            ds.AddModelPathPrefix(updatedMapping.CurrentDataSourceMemberInfo.Name);
                        }
                    }
                }
                this.newJoinedDataSource.AddModelPathPrefix(updatedMapping.NewDataSourceMemberInfo.Name);
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            SqlExpression joinCondition = null;
            // -1 is because 1st arg is always removed by base class, usually SqlExpression[] has
            // sqlQuery as 1st arg, but base class removes it and pass it in the first argument, however, the original
            // LINQ Expression has the sqlQuery in the 1st argument, that's why we are doing -1 here.
            var joinConditionIndex = this.GetJoinConditionIndex() - 1;
            if (joinConditionIndex >= 0)
                joinCondition = arguments[joinConditionIndex];
            var otherDataSource = arguments[0];
            SqlJoinType joinType = this.GetJoinType();
            var ds = this.newJoinedDataSource
                        ?? (otherDataSource as SqlDataSourceReferenceExpression)?.DataSource as SqlDataSourceExpression
                        ?? throw new InvalidOperationException($"2nd argument of Join Query Method must be a {nameof(SqlDataSourceExpression)}.");
            var joinExpression = this.SqlFactory.CreateJoin(joinType, ds, joinCondition);
            sqlQuery.ApplyJoin(joinExpression);
            return sqlQuery;
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the join.
        ///     </para>
        /// </summary>
        /// <returns>The type of the join.</returns>
        protected virtual SqlJoinType GetJoinType()
        {
            var methodCallExpression = this.Expression;
            SqlJoinType joinType = SqlJoinType.Inner;
            if (methodCallExpression.Method.Name == nameof(QueryExtensions.LeftJoin))
                joinType = SqlJoinType.Left;
            else if (methodCallExpression.Method.Name == nameof(QueryExtensions.RightJoin))
                joinType = SqlJoinType.Right;
            else if (methodCallExpression.Method.Name == nameof(QueryExtensions.CrossApply))
                joinType = SqlJoinType.CrossApply;
            else if (methodCallExpression.Method.Name == nameof(QueryExtensions.OuterApply))
                joinType = SqlJoinType.OuterApply;
            else if (methodCallExpression.Method.Name == nameof(QueryExtensions.FullOuterJoin))
                joinType = SqlJoinType.FullOuter;
            return joinType;
        }
    }
}
