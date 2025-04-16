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
        protected abstract SqlExpression CreateDmSqlExpression(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource, SqlExpression[] arguments);

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            // Type-1: Update(query, tableUpdateFields, predicate)                      arg count = 3
            // Type-2: Update(query, tableSelection, tableUpdateFields, predicate)      arg count = 4
            if (HasTableSelection)
            {
                if (sourceExpression == this.TableSelectionArgument)       // tableSelection argument
                {
                    if (this.SourceQuery.Projection != null)                // projection has been applied
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

                            var projections = this.SourceQuery.Projection.GetProjections();
                            var matchedColumns = projections.Where(x => x.ModelPath.StartsWith(path)).ToArray();
                            if (matchedColumns.Length > 0 &&
                                matchedColumns.All(x => x.ColumnExpression is SqlDataSourceColumnExpression))
                            {
                                if (matchedColumns.GroupBy(x => ((SqlDataSourceColumnExpression)x.ColumnExpression).DataSource).Count() == 1)
                                {
                                    var ds = ((SqlDataSourceColumnExpression)matchedColumns.First().ColumnExpression).DataSource;
                                    convertedExpression = new SqlDataSourceReferenceExpression(ds);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return base.TryOverrideChildConversion(sourceExpression, out convertedExpression);
        }

        /// <inheritdoc />
        protected override SqlExpression Convert(SqlQueryExpression sqlQuery, SqlExpression[] arguments)
        {
            SqlDataSourceExpression selectedDataSource;
            if (HasTableSelection)
            {
                var sqlExpressionArgIndex = this.TableSelectionArgumentIndex - 1;
                
                selectedDataSource = (arguments[sqlExpressionArgIndex] as SqlDataSourceReferenceExpression)?.Reference
                                        ??
                                        throw new InvalidOperationException($"The arg-{sqlExpressionArgIndex} of the {nameof(QueryExtensions.Update)} method must be a data source. Make sure arg-{sqlExpressionArgIndex} is a {nameof(SqlDataSourceReferenceExpression)}. Current type is '{arguments[sqlExpressionArgIndex].GetType()}'.");
            }
            else
                selectedDataSource = sqlQuery.InitialDataSource;

            if (sqlQuery.Projection != null)
            {
                // E.g., From(t1 , t2).Join(t1 and t2).Where(filter applied on t1 or t2).Select(new {t1,t2}).Update(t2, t2 fields, filter on t1)
                // In above example, in `Update` method we'll receive `t2` as table to update, but since we have `Select` method applied before
                // that therefore, `t2` will not be converted as Data Source, but it's already been handled in above `TryOverrideChildConversion` method.
                // However, we have `Where` getting applied below, that's the problematic part,
                // since `Select` has already been applied, therefore, this `Where` application 
                // will cause the wrapping of the query which pushes down the `t2` data source and it will
                // become invalid data source. 
                // That's why we are Undoing Projection here so that `Where` does not wrap the query
                // and `t2` remains as a valid data source.

                sqlQuery.UndoProjection();
            }

            var predicate = arguments[this.WherePredicateArgumentIndex - 1];
            sqlQuery.ApplyWhere(predicate);

            return this.CreateDmSqlExpression(sqlQuery, selectedDataSource, arguments);
        }
    }
}
