using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
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

        private Guid? newlyJoinedDataSourceAlias;

        /// <inheritdoc />
        protected override void OnArgumentConverted(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression argument, SqlExpression convertedArgument)
        {
            var argIndex = this.Expression.Arguments.IndexOf(argument);
            if (this.GetNewlyJoinedDataSourceIndex() == argIndex)
            {
                var derivedTable = convertedArgument as SqlDerivedTableExpression
                                    ??
                                    throw new InvalidOperationException($"2nd argument of Join Query Method must be a {nameof(SqlDerivedTableExpression)}, currently it's being converted to {convertedArgument.GetType().Name}.");
                var joinedQuery = derivedTable.ConvertToTableIfPossible();
                var ds = this.SourceQuery.AddJoin(joinedQuery, SqlJoinType.Cross);
                this.newlyJoinedDataSourceAlias = ds.DataSourceAlias;

                var newShapeLambda = this.GetArgumentLambda(this.GetNewExpressionIndex());
                // The base converter class `QueryMethodExpressionConverter` automatically maps the first argument 
                // of all LambdaExpressions in this method call. However, we need to manually map the second argument 
                // because the base class does not handle it. 
                // The base class removes all mapped parameters at the end, not just the first parameter, 
                // so we do not need to explicitly remove the second parameter mapping ourselves.
                this.ParameterMap.TrySetParameterMap(newShapeLambda.Parameters[1], ds);
            }
            else if (this.GetNewExpressionIndex() == argIndex)
            {
                var compositeBindingExpression = convertedArgument as SqlCompositeBindingExpression 
                                                    ??
                                                    throw new InvalidOperationException($"3rd argument of Join Query Method must be a {nameof(SqlCompositeBindingExpression)}.");
                this.SourceQuery.UpdateModelBinding(compositeBindingExpression);
            }
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlSelectExpression selectQuery, SqlExpression[] arguments)
        {
            var joinCondition = this.GetJoinCondition(arguments);
            var joinType = this.GetJoinType();
            Guid joinedDataSourceAlias;
            if (this.newlyJoinedDataSourceAlias != null)
            {
                // if we are here it means a separate query was provided in join method
                // to be added on the fly
                joinedDataSourceAlias = this.newlyJoinedDataSourceAlias.Value;
            }
            else
            {
                // if we are here it means a data source was already added through "From" method

                // below index will be returned as per the MethodCallExpression while we are trying to
                // get the argument from `arguments` array (method argument), which will not have SqlSelectExpression
                var alreadyAvailableDataSourceArgIndex = this.GetAlreadyAvailableDataSourceIndex() - 1;
                var alreadyAvailableDataSource = arguments[alreadyAvailableDataSourceArgIndex] as SqlDataSourceExpression
                                                    ??
                                                    throw new InvalidOperationException($"2nd argument of Join Query Method must be a {nameof(SqlDataSourceExpression)}.");
                joinedDataSourceAlias = alreadyAvailableDataSource.DataSourceAlias;
            }

            this.SourceQuery.UpdateJoin(joinedDataSourceAlias, joinType, joinCondition, joinName: null, navigationJoin: false, navigationParent: null);

            return selectQuery;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected virtual SqlExpression GetJoinCondition(SqlExpression[] arguments)
        {
            // -1 is because 1st arg is always removed by base class, usually SqlExpression[] has
            // sqlQuery as 1st arg, but base class removes it and pass it in the first argument, however, the original
            // LINQ Expression has the sqlQuery in the 1st argument, that's why we are doing -1 here.
            var joinConditionIndex = this.GetJoinConditionIndex() - 1;
            if (joinConditionIndex >= 0)
                return arguments[joinConditionIndex];
            return null;
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
