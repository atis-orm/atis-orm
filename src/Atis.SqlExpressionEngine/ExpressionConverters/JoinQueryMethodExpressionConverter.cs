using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Data;
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
    public class JoinQueryMethodExpressionConverter : LinqToSqlQueryConverterBase<MethodCallExpression>
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
        
        private int GetNewlyJoinedDataSourceArgIndex()
        {
            if (this.ArgCount == 4)
                return 1;
            else if (this.ArgCount == 3 && this.IsCrossOrOuterApply())
                return 1;
            return -1;
        }
        
        private int GetAvailableDataSourceSelectionArgIndex()
        {
            if (this.ArgCount == 3 && !this.IsCrossOrOuterApply())
                return 1;
            return -1;
        }
        
        private int GetNewExpressionArgIndex()
        {
            if (this.ArgCount == 4)
                return 2;
            else if (this.ArgCount == 3 && this.IsCrossOrOuterApply())
                return 2;
            return -1;
        }
        
        private int GetJoinConditionArgIndex()
        {
            if (this.ArgCount == 4)
                return 3;
            if (this.ArgCount == 3 && !this.IsCrossOrOuterApply())
                return 2;
            return -1;
        }

        private Guid? newlyJoinedDataSourceAlias;
        private SqlSelectExpression sourceQuery;

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedArgument)
        {
            var argIndex = this.Expression.Arguments.IndexOf(childNode);
            if (argIndex == 0)          // source query
            {
                this.sourceQuery = convertedArgument.CastTo<SqlSelectExpression>();
                this.sourceQuery.WrapIfRequired(SqlQueryOperation.Join);
                if (this.GetAvailableDataSourceSelectionArgIndex() >= 0)
                {
                    // for cross apply or left join with a separate data source being added
                    // we receive a parameter
                    var arg1Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 1, paramIndex: 0);
                    this.MapParameter(arg1Param0, () => this.sourceQuery.GetQueryShapeForDataSourceMapping());

                    this.MapJoinConditionLambdaParameterIfRequired();
                }
                else if (this.IsCrossOrOuterApply())
                {
                    var arg1Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 1, paramIndex: 0);
                    this.MapParameter(arg1Param0, () => this.sourceQuery.GetQueryShapeForFieldMapping());
                }
            }
            else if (this.GetNewlyJoinedDataSourceArgIndex() == argIndex)
            {
                var derivedTable = convertedArgument.CastTo<SqlDerivedTableExpression>();
                var joinedQuery = derivedTable.ConvertToTableIfPossible();
                var dsQueryShape = this.sourceQuery.AddJoin(joinedQuery, SqlJoinType.Cross);
                this.newlyJoinedDataSourceAlias = dsQueryShape.DataSourceAlias;
                var arg2Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: this.GetNewExpressionArgIndex(), paramIndex: 0);
                var arg2Param1 = this.Expression.GetArgLambdaParameterRequired(argIndex: this.GetNewExpressionArgIndex(), paramIndex: 1);
                this.MapParameter(arg2Param0, () => this.sourceQuery.GetQueryShapeForDataSourceMapping());
                this.MapParameter(arg2Param1, () => dsQueryShape);
            }
            else if (this.GetNewExpressionArgIndex() == argIndex)
            {
                var newQueryShape = convertedArgument.CastTo<SqlMemberInitExpression>($"3rd Argument (Arg-2) of {this.Expression.Method.Name} method must be a {nameof(NewExpression)}.");
                this.sourceQuery.UpdateModelBinding(newQueryShape);
                this.MapJoinConditionLambdaParameterIfRequired();
            }
            base.OnConversionCompletedByChild(childConverter, childNode, convertedArgument);
        }

        private void MapJoinConditionLambdaParameterIfRequired()
        {
            if (this.GetJoinConditionArgIndex() >= 0)
            {
                var arg3Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: this.GetJoinConditionArgIndex(), paramIndex: 0);
                // NOTE: here we are doing GetQueryShapeForFieldMapping instead of GetQueryShapeForDataSourceMapping
                // this is important
                this.MapParameter(arg3Param0, () => this.sourceQuery.GetQueryShapeForFieldMapping());
            }
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var joinCondition = this.GetJoinCondition(convertedChildren);
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
                var alreadyAvailableDataSourceArgIndex = this.GetAvailableDataSourceSelectionArgIndex();
                var dsQueryShape = convertedChildren[alreadyAvailableDataSourceArgIndex].CastTo<SqlDataSourceQueryShapeExpression>("The data source selection in the join method must be a data source, make sure projection has not been applied and you are not selecting an item from projection.");
                joinedDataSourceAlias = dsQueryShape.DataSourceAlias;
            }

            // joinCondition can be null in-case of outer / cross apply
            this.sourceQuery.UpdateJoin(joinedDataSourceAlias, joinType, joinCondition, joinName: null, navigationJoin: false);

            return this.sourceQuery;
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
            var joinConditionIndex = this.GetJoinConditionArgIndex();
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
