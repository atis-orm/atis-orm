using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public abstract class DataManipulationQueryMethodExpressionConverterBase : LinqToSqlQueryConverterBase<MethodCallExpression>
    {
        private SqlSelectExpression sourceQuery;

        protected DataManipulationQueryMethodExpressionConverterBase(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        protected abstract bool HasTableSelection { get; }
        protected virtual Expression TableSelectionArgument => this.Expression.Arguments[this.TableSelectionArgumentIndex];
        protected virtual int TableSelectionArgumentIndex => 1;
        protected abstract int WherePredicateArgumentIndex { get; }
        protected abstract SqlExpression CreateDmSqlExpression(SqlDerivedTableExpression sqlQuery, Guid selectedDataSource, IReadOnlyList<SqlExpression> arguments);

        public override bool IsChainedQueryArgument(Expression childNode) => childNode == this.Expression.Arguments[0];

        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments[0])       // source query
            {
                this.sourceQuery = convertedExpression.CastTo<SqlSelectExpression>();

                for (var i = 1; i < this.Expression.Arguments.Count; i++)
                {
                    if (this.Expression.TryGetArgLambdaParameter(argIndex: i, paramIndex: 0, out var argParam))
                    {
                        if (i == this.TableSelectionArgumentIndex)
                            this.MapParameter(argParam, () => this.sourceQuery.GetQueryShapeForDataSourceMapping());
                        else
                            this.MapParameter(argParam, () => this.sourceQuery.GetQueryShapeForFieldMapping());
                    }
                }
            }
            base.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            Guid dataSourceToUpdate;
            if (this.HasTableSelection)
            {
                dataSourceToUpdate = convertedChildren[this.TableSelectionArgumentIndex]
                                        .CastTo<SqlDataSourceQueryShapeExpression>().DataSourceAlias;
            }
            else
            {
                dataSourceToUpdate = this.sourceQuery.DataSources.First().Alias;
            }

            var predicate = convertedChildren[this.WherePredicateArgumentIndex];
            this.sourceQuery.ApplyWhere(predicate, useOrOperator: false);

            var derivedTable = this.SqlFactory.ConvertSelectQueryToDataManipulationDerivedTable(this.sourceQuery);

            return this.CreateDmSqlExpression(derivedTable, dataSourceToUpdate, convertedChildren.Skip(1).ToArray());
        }
    }
}
