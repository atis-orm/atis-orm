using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public abstract class DataManipulationQueryMethodExpressionConverterBase : QueryMethodExpressionConverterBase
    {
        protected DataManipulationQueryMethodExpressionConverterBase(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        protected abstract bool HasTableSelection { get; }
        protected virtual Expression TableSelectionArgument => this.Expression.Arguments[this.TableSelectionArgumentIndex];
        protected virtual int TableSelectionArgumentIndex => 1;
        protected abstract int WherePredicateArgumentIndex { get; }
        protected abstract SqlExpression CreateDmSqlExpression(SqlDerivedTableExpression sqlQuery, Guid selectedDataSource, SqlExpression[] arguments);

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            // Type-1: Update(query, tableUpdateFields, predicate)                      arg count = 3
            // Type-2: Update(query, tableSelection, tableUpdateFields, predicate)      arg count = 4
            if (HasTableSelection)
            {
                if (sourceExpression == this.TableSelectionArgument)       // tableSelection argument
                {
                    if (this.SourceQuery.HasProjectionApplied)                // projection has been applied
                    {
                        /* usually this can happen if two data sources are directly selected in projection
                         * e.g.                       
                            (
                                from asset in assets
                                join item in items on asset.ItemId equals item.ItemId
                                where asset.SerialNumber == "123"
                                select new { asset, item }      <- here projection will be applied on SqlQueryExpression
                            )
                            .Update(ms => ms.item, ms => new ItemBase { ItemDescription = ms.item.ItemDescription + ms.asset.SerialNumber }, ms => ms.asset.SerialNumber == "123");

                        so here `ms` will be an object pointing to a SqlQueryExpression where Projection has already been applied
                        therefore, below `ms.item` MemberExpression will not be translated into data source, rather it will select all the fields of `item`,
                        that's why we are overriding the child conversion so that instead of returning the columns of `item`
                        we'll return the `item` as data source itself.
                        */

                        if (((sourceExpression as UnaryExpression)?.Operand as LambdaExpression)?.Body is MemberExpression memberExpression)
                        {
                            // we will be here for example in the above case for `ms.item`
                            var path = memberExpression.GetModelPath();

                            var projections = this.SourceQuery.SelectList;
                            var matchedColumns = projections.Where(x => x.ModelPath.StartsWith(path)).ToArray();
                            var columnDataSources = GetColumnExpressionDataSources(matchedColumns);
                            if (columnDataSources.Length == 1)
                            {
                                convertedExpression = new SqlDataSourceExpression(this.SourceQuery, columnDataSources[0]);
                                return true;
                            }
                        }
                    }
                }
            }
            return base.TryOverrideChildConversion(sourceExpression, out convertedExpression);
        }

        protected override SqlExpression Convert(SqlSelectExpression sqlQuery, SqlExpression[] arguments)
        {
            Guid? dataSourceToUpdate = null;
            if (this.HasTableSelection)
            {
                dataSourceToUpdate = (arguments[0] as SqlDataSourceExpression)?.DataSourceAlias
                                        ??
                                        throw new InvalidOperationException($"Arg-1 of '{this.Expression.Method.Name}' is not a {nameof(SqlDataSourceExpression)}.");
            }
            else
            {
                dataSourceToUpdate = sqlQuery.DataSources.First().Alias;
            }

            // undoing the projection so that query should not wrap when applying where
            if (sqlQuery.HasProjectionApplied)
                sqlQuery.UndoProjection();

            var predicate = arguments[this.WherePredicateArgumentIndex - 1];
            sqlQuery.ApplyWhere(predicate, useOrOperator: false);

            var derivedTable = this.SqlFactory.ConvertSelectQueryToDataManipulationDerivedTable(sqlQuery);
            
            return this.CreateDmSqlExpression(derivedTable, dataSourceToUpdate.Value, arguments);
        }

        private static Guid[] GetColumnExpressionDataSources(SelectColumn[] columnExpressions)
        {
            if (columnExpressions?.Length > 0 &&
                    columnExpressions.All(x => x.ColumnExpression is SqlDataSourceColumnExpression))
            {
                return columnExpressions.GroupBy(x => ((SqlDataSourceColumnExpression)x.ColumnExpression).DataSourceAlias).Select(x => x.Key).ToArray();
            }
            return Array.Empty<Guid>();
        }
    }
}
