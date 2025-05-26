using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// Represents a Table, Derived Table / Sub-Query, or CTE Reference within a select query.
    /// </summary>
    public abstract class SqlQuerySourceExpression : SqlExpression
    {
        /// <summary>
        /// Returns the set of column names along with model path that query source is projecting.
        /// </summary>
        /// <returns>Unique instances of <see cref="ColumnModelPath"/>.</returns>
        public abstract SqlDataSourceQueryShapeExpression CreateQueryShape(Guid dataSourceAlias);

        protected SqlExpression UpdateQueryShapeWithNewAlias(SqlQueryShapeExpression outerQueryShape_a, Guid newDataSourceAlias, IReadOnlyList<SelectColumn> selectColumns)
        {
            var foundCols = new List<SelectColumn>();
            var result = internalFunction(outerQueryShape_a);
            if (result is SqlMemberInitExpression resultAsMemberInit)
            {
                var notFoundCols = selectColumns.Except(foundCols).ToArray();
                foreach (var notFoundCol in notFoundCols)
                {
                    resultAsMemberInit.AddMemberAssignment(notFoundCol.Alias, new SqlDataSourceColumnExpression(newDataSourceAlias, notFoundCol.Alias), projectable: true);
                }
            }
            return result;

            SqlExpression internalFunction(SqlQueryShapeExpression outerQueryShape)
            {
                var memberAssignments = new List<SqlMemberAssignment>();
                SqlMemberInitExpression memberInit;
                if (outerQueryShape is SqlDataSourceQueryShapeExpression qds)
                {
                    if (qds.ShapeExpression is SqlMemberInitExpression mi)
                        memberInit = mi;
                    else
                        return qds.ShapeExpression;
                }
                else if (outerQueryShape is SqlMemberInitExpression mi2)
                    memberInit = mi2;
                else
                    throw new InvalidOperationException($"outerQueryShape type '{outerQueryShape.GetType().Name}' is not supported.");

                foreach (var binding in memberInit.Bindings.Where(x => x.Projectable))
                {
                    if (!(binding.SqlExpression is SqlQueryableExpression))
                    {
                        SqlMemberAssignment memberAssignment;
                        if (binding.SqlExpression is SqlMemberInitExpression innerQs)
                        {
                            memberAssignment = new SqlMemberAssignment(binding.MemberName, internalFunction(innerQs));
                        }
                        else if (binding.SqlExpression is SqlDataSourceQueryShapeExpression dsQueryShape)
                        {
                            memberAssignment = new SqlMemberAssignment(binding.MemberName, internalFunction(dsQueryShape));
                        }
                        else
                        {
                            var selectCol = selectColumns.Where(x => x.ColumnExpression == binding.SqlExpression).OrderBy(x => x.Alias == binding.MemberName ? 0 : 1).FirstOrDefault()
                                                    ??
                                                    throw new InvalidOperationException($"Cannot find column for expression {binding.SqlExpression} in select columns. The comparison was done by Reference, check the derived table creation process, the column expressions in QueryShape must be equal to Select Column List, if not possible then we might need to change comparison here to Hash Comparison.");
                            foundCols.Add(selectCol);
                            memberAssignment = new SqlMemberAssignment(binding.MemberName, new SqlDataSourceColumnExpression(newDataSourceAlias, selectCol.Alias), projectable: binding.Projectable);
                        }
                        memberAssignments.Add(memberAssignment);
                    }
                }
                return new SqlMemberInitExpression(memberAssignments);
            }
        }
    }

    public abstract class SqlSubQuerySourceExpression : SqlQuerySourceExpression
    {
    }
}
