using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
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
        
        /// <inheritdoc />
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
        private SqlSelectExpression sourceQuery;
        private SqlDataSourceQueryShapeExpression joinedDataSourceQueryShape;
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
                this.sourceQuery = convertedExpression.CastTo<SqlSelectExpression>($"Arg-{this.SourceColumnsArgIndex} of {this.Expression.Method.Name} must be converted to {nameof(SqlSelectExpression)}.");

                var arg2Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 2, paramIndex: 0);
                var arg4Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 4, paramIndex: 0);

                this.MapParameter(arg2Param0, () => sourceQuery.GetQueryShapeForFieldMapping());
                this.MapParameter(arg4Param0, () => sourceQuery.GetQueryShapeForFieldMapping());
            }
            else if (childNode == this.Expression.Arguments[this.OtherDataArgIndex])        // other data source converted
            {
                bool isDefaultIfEmpty = false;
                if (convertedExpression is SqlDefaultIfEmptyExpression defaultIfEmpty)
                {
                    convertedExpression = defaultIfEmpty.DerivedTable;
                    isDefaultIfEmpty = true;
                }
                var otherQuerySource = convertedExpression.CastTo<SqlQuerySourceExpression>($"Arg-{this.OtherDataArgIndex} of {this.Expression.Method.Name} must be converted to {nameof(SqlQuerySourceExpression)}.");
                if (otherQuerySource is SqlDerivedTableExpression derivedTable)
                    otherQuerySource = derivedTable.ConvertToTableIfPossible();

                if (this.IsGroupJoin)
                {
                    // below will automatically convert the derived table to SqlSelectQuery correctly if possible
                    // which means it's NOT going to keep wrapping
                    this.otherSelectQuery = this.SqlFactory.CreateSelectQuery(otherQuerySource);

                    var arg3Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 3, paramIndex: 0);
                    this.MapParameter(arg3Param0, () => this.otherSelectQuery.GetQueryShapeForFieldMapping());
                }
                else
                {
                    var joinType = isDefaultIfEmpty ? SqlJoinType.Left : SqlJoinType.Inner;
                    this.joinedDataSourceQueryShape = this.sourceQuery.AddJoin(otherQuerySource, joinType);

                    var arg3Param0 = this.Expression.GetArgLambdaParameterRequired(argIndex: 3, paramIndex: 0);
                    var arg4Param1 = this.Expression.GetArgLambdaParameterRequired(argIndex: 4, paramIndex: 1);

                    this.MapParameter(arg3Param0, () => this.joinedDataSourceQueryShape);
                    this.MapParameter(arg4Param1, () => this.joinedDataSourceQueryShape);
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

                var joinCondition = this.SqlFactory.CreateJoinCondition(this.sourceColumn, convertedExpression);
                this.otherSelectQuery.ApplyWhere(joinCondition, useOrOperator: false);
            }
        }

        /// <inheritdoc />
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
                // `QueryShape` property. Later when this SqlDerivedTableExpression is accessed from
                // `QueryShape` it will be received as a independent expression and will be rendered
                // as sub-query.
                var otherDerivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(this.otherSelectQuery);

                var arg4Param1 = this.Expression.GetArgLambdaParameterRequired(argIndex: 4, paramIndex: 1);
                this.MapParameter(arg4Param1, () => otherDerivedTable);
            }
            base.OnBeforeChildVisit(childNode);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // convertedChildren[0] = source query
            // convertedChildren[1] = other query to join
            // convertedChildren[2] = source query PK selection            
            // convertedChildren[3] = other query FK selection            
            // convertedChildren[4] = new shape

            if (!this.IsGroupJoin)
            {
                if (this.joinedDataSourceQueryShape is null)
                    throw new InvalidOperationException($"joinedDataSourceQueryShape is null");
                var leftSide = convertedChildren[2];
                var rightSide = convertedChildren[3];
                var joinCondition = this.SqlFactory.CreateJoinCondition(leftSide, rightSide);
                this.sourceQuery.UpdateJoinCondition(this.joinedDataSourceQueryShape.DataSourceAlias, joinCondition);
            }

            var newShape = convertedChildren[4].CastTo<SqlQueryShapeExpression>($"Last argument of {this.Expression.Method.Name} was not converted to {nameof(SqlQueryShapeExpression)}.");

            if (this.IsGroupJoin)
            {
                if (this.IsDefaultProjection())
                {
                    this.sourceQuery.UpdateModelBinding(newShape);
                }
                else
                {
                    this.sourceQuery.ApplyProjection(newShape);
                }
            }
            else
            {
                this.sourceQuery.UpdateModelBinding(newShape);
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
        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];
    }
}
