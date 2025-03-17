namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a data source in an SQL query expression tree.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A query source is an expression that can be used in From and Join
    ///         parts of the query. Such as a table, subquery, or a CTE reference.
    ///     </para>
    ///     <para>
    ///         This class provides a flag that can indicate the source query that
    ///         when joining this data source, it should be added as an outer join.
    ///     </para>
    /// </remarks>
    public abstract class SqlQuerySourceExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Gets and sets flag to notify the SQL Query that it should be added as outer join.
        ///     </para>
        /// </summary>
        public virtual bool IsDefaultIfEmpty { get; set; }
    }
}
