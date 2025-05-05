using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating LINQ's Join method.
    ///     </para>
    /// </summary>
    public class StandardJoinQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
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

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                (methodCallExpression.Method.Name == nameof(Queryable.Join) ||
                 methodCallExpression.Method.Name == nameof(Queryable.GroupJoin)) &&
                    (methodCallExpression.Method.DeclaringType == typeof(Queryable) ||
                    methodCallExpression.Method.DeclaringType == typeof(Enumerable)))
            {
                converter = new StandardJoinQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;   
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for standard join query methods.
    ///     </para>
    /// </summary>
    public class StandardJoinQueryMethodExpressionConverter : LinqToSqlQueryConverterBase<MethodCallExpression>
    {
        private readonly ILambdaParameterToDataSourceMapper lambdaParameterMap;
        private SqlSelectExpression sourceQuery;
        private ParameterExpression sourceLambdaParameterArg2;
        private ParameterExpression sourceLambdaParameterArg4;
        private Guid? join;
        private ParameterExpression otherDataLambdaParameterArg3;
        private ParameterExpression otherDataLambdaParameterArg4;
        private SqlSelectExpression otherSelectQuery;
        private SqlExpression sourceColumn;

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
            this.lambdaParameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
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

        private int SourceQueryArgIndex => 0;
        private int OtherDataArgIndex => 1;
        private int SourceColumnsArgIndex => 2;
        private int OtherColumnsArgIndex => 3;
        private int SelectArgIndex => 4;

        private bool IsGroupJoin => this.Expression.Method.Name == nameof(Queryable.GroupJoin);

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments[this.SourceQueryArgIndex])  // main query is converted
            {
                this.sourceQuery = convertedExpression as SqlSelectExpression
                                    ??
                                    throw new InvalidOperationException($"Expected '{nameof(SqlSelectExpression)}' but got '{convertedExpression.GetType().Name}'.");

                this.sourceLambdaParameterArg2 = this.GetArgLambdaParameter(2, 0);
                this.sourceLambdaParameterArg4 = this.GetArgLambdaParameter(4, 0);

                this.lambdaParameterMap.TrySetParameterMap(sourceLambdaParameterArg2, sourceQuery);
                this.lambdaParameterMap.TrySetParameterMap(sourceLambdaParameterArg4, sourceQuery);
            }
            else if (childNode == this.Expression.Arguments[this.OtherDataArgIndex])        // other data source converted
            {
                bool isDefaultIfEmpty = false;
                if (convertedExpression is SqlDefaultIfEmptyExpression defaultIfEmpty)
                {
                    convertedExpression = defaultIfEmpty.DerivedTable;
                    isDefaultIfEmpty = true;
                }
                var otherQuerySource = convertedExpression as SqlQuerySourceExpression
                                        ??
                                        throw new InvalidOperationException($"Expected '{nameof(SqlQuerySourceExpression)}' but got '{convertedExpression.GetType().Name}'.");
                if (otherQuerySource is SqlDerivedTableExpression derivedTable)
                    otherQuerySource = derivedTable.ConvertToTableIfPossible();

                SqlDataSourceExpression dataSource;
                if (this.IsGroupJoin)
                {
                    // below will automatically convert the derived table to SqlSelectQuery correctly if possible
                    // which means it's NOT going to keep wrapping
                    this.otherSelectQuery = this.SqlFactory.CreateSelectQueryFromQuerySource(otherQuerySource);

                    this.otherDataLambdaParameterArg3 = this.GetArgLambdaParameter(3, 0);

                    this.lambdaParameterMap.TrySetParameterMap(otherDataLambdaParameterArg3, this.otherSelectQuery);
                }
                else
                {
                    var joinType = isDefaultIfEmpty ? SqlJoinType.Left : SqlJoinType.Inner;
                    dataSource = this.sourceQuery.AddJoin(otherQuerySource, joinType);
                    this.join = dataSource.DataSourceAlias;

                    this.otherDataLambdaParameterArg3 = this.GetArgLambdaParameter(3, 0);
                    this.otherDataLambdaParameterArg4 = this.GetArgLambdaParameter(4, 1);

                    this.lambdaParameterMap.TrySetParameterMap(otherDataLambdaParameterArg3, dataSource);
                    this.lambdaParameterMap.TrySetParameterMap(otherDataLambdaParameterArg4, dataSource);

                    this.sourceLambdaParameterArg4 = this.GetArgLambdaParameter(4, 0);
                    this.lambdaParameterMap.TrySetParameterMap(this.sourceLambdaParameterArg4, dataSource);
                }
            }
            else if (this.IsGroupJoin && childNode == this.Expression.Arguments[this.SourceColumnsArgIndex])
            {
                this.sourceColumn = convertedExpression;
            }
            else if (this.IsGroupJoin && childNode == this.Expression.Arguments[this.OtherColumnsArgIndex])
            {
                if (this.otherSelectQuery is null)
                    throw new InvalidOperationException($"{nameof(otherSelectQuery)} is null");

                var joinCondition = this.CreateJoinCondition(this.sourceColumn, convertedExpression);
                this.otherSelectQuery.ApplyWhere(joinCondition, useOrOperator: false);
            }
        }

        public override void OnBeforeChildVisit(Expression childNode)
        {
            if (this.IsGroupJoin && childNode == this.Expression.Arguments[this.SelectArgIndex])
            {
                // Preparing to visit the 5th argument, which is a NewExpression.
                // In the case of GroupJoin, the other data source is converted to a SelectQuery 
                // to allow adding a WHERE condition. However, when visiting the 5th argument, 
                // typically a NewExpression, the new data source is selected in the NewExpression.
                // This selection should be treated as a separate derived table in the query, 
                // rather than as a joined data source, unless a SelectMany follows the GroupJoin.
                // To achieve this, the SqlSelectExpression is converted to a SqlDerivedTableExpression, 
                // ensuring it is added as a separate independent SqlExpression in the SqlSelectExpression's 
                // `modelBinding` property. Later when this SqlDerivedTableExpression is accessed from
                // `modelBinding` it will be received as a independent expression and will be rendered
                // as sub-query.
                var otherDerivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(this.otherSelectQuery);

                this.otherDataLambdaParameterArg4 = this.GetArgLambdaParameter(4, 1);
                this.lambdaParameterMap.TrySetParameterMap(this.otherDataLambdaParameterArg4, otherDerivedTable);
            }
            base.OnBeforeChildVisit(childNode);
        }

        private ParameterExpression GetArgLambdaParameter(int argIndex, int parameterIndex)
        {
            var argument = this.Expression.Arguments[argIndex];
            var lambda = (argument as UnaryExpression)?.Operand as LambdaExpression
                            ??
                            argument as LambdaExpression
                            ??
                            throw new InvalidOperationException($"Expected '{nameof(LambdaExpression)}' but got '{argument.GetType().Name}'.");
            return lambda.Parameters[parameterIndex];
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // convertedChildren[0] = source query
            // convertedChildren[1] = other query to join
            // convertedChildren[2] = source query PK selection            
            // convertedChildren[3] = other query FK selection            
            // convertedChildren[4] = new shape

            SqlCompositeBindingExpression newShape;
            if (convertedChildren[4] is SqlCompositeBindingExpression compositeBinding)
                newShape = compositeBinding;
            else
                newShape = this.SqlFactory.CreateCompositeBindingForSingleExpression(convertedChildren[4], ModelPath.Empty);

            if (!this.IsGroupJoin)
            {
                if (join is null)
                    throw new InvalidOperationException($"join is null");
                var leftSide = convertedChildren[2];
                var rightSide = convertedChildren[3];
                var joinCondition = this.CreateJoinCondition(leftSide, rightSide);
                this.sourceQuery.UpdateJoinCondition(this.join.Value, joinCondition);
            }
            
            this.sourceQuery.UpdateModelBinding(newShape);

            if (this.IsGroupJoin)
            {
                if (this.IsDefaultProjection())
                {
                    this.sourceQuery.MarkModelBindingAsNonProjectable(newShape.Bindings[1].ModelPath);
                }
                else
                {
                    this.sourceQuery.ApplyProjection(newShape);
                }
            }

            return this.sourceQuery;
        }

        private bool IsDefaultProjection()
        {
            if (this.Expression.TryGetArgLambda(this.SelectArgIndex, out var lambda))
            {
                if (lambda.Body is NewExpression newExpression)
                {
                    if (newExpression.Arguments.Count == 2 &&
                        newExpression.Arguments[0] == lambda.Parameters[0] &&
                        newExpression.Arguments[1] == lambda.Parameters[1])
                        return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public override void OnAfterVisit()
        {
            this.lambdaParameterMap.RemoveParameterMap(this.sourceLambdaParameterArg2);
            this.lambdaParameterMap.RemoveParameterMap(this.sourceLambdaParameterArg4);
            this.lambdaParameterMap.RemoveParameterMap(this.otherDataLambdaParameterArg3);
            this.lambdaParameterMap.RemoveParameterMap(this.otherDataLambdaParameterArg4);
            base.OnAfterVisit();
        }

        private SqlExpression CreateJoinCondition(SqlExpression leftSide, SqlExpression rightSide)
        {
            SqlBinaryExpression joinPredicate = null;

            if (leftSide is SqlCompositeBindingExpression leftComposite)
            {
                if (rightSide is SqlCompositeBindingExpression rightComposite)
                {
                    if (leftComposite.Bindings.Length != rightComposite.Bindings.Length)
                        throw new InvalidOperationException($"Source columns count {leftComposite.Bindings.Length} does not match other columns count {rightComposite.Bindings.Length}.");

                    for (var i = 0; i < leftComposite.Bindings.Length; i++)
                    {
                        var leftSideBinding = leftComposite.Bindings[i];
                        var rightSideBinding = rightComposite.Bindings[i];
                        var sourceColumn = leftSideBinding.SqlExpression;
                        var otherColumn = rightSideBinding.SqlExpression;
                        var condition = this.SqlFactory.CreateBinary(sourceColumn, otherColumn, SqlExpressionType.Equal);
                        joinPredicate = joinPredicate == null ? condition : this.SqlFactory.CreateBinary(joinPredicate, condition, SqlExpressionType.AndAlso);
                    }
                }
                else
                    throw new InvalidOperationException($"Expected {nameof(SqlCompositeBindingExpression)} for other columns selection Arg-Index: {this.OtherColumnsArgIndex}.");
            }
            else
            {
                joinPredicate = this.SqlFactory.CreateBinary(leftSide, rightSide, SqlExpressionType.Equal);
            }

            return joinPredicate;
        }

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];
    }
}
