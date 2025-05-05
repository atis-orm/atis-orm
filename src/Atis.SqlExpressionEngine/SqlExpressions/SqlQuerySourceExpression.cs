using System.Collections.Generic;

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
        public abstract HashSet<ColumnModelPath> GetColumnModelMap();
    }

    public abstract class SqlSubQuerySourceExpression : SqlQuerySourceExpression
    {
    }
}
