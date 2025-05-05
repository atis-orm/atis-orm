namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public enum SqlJoinType
    {
        /// <summary> Represents a LEFT JOIN, returning all records from the left table and matching records from the right table. </summary>
        Left,
        /// <summary> Represents a RIGHT JOIN, returning all records from the right table and matching records from the left table. </summary>
        Right,
        /// <summary> Represents an INNER JOIN, returning only records that have matching values in both tables. </summary>
        Inner,
        /// <summary> Represents a CROSS JOIN, producing the Cartesian product of both tables. </summary>
        Cross,
        /// <summary> Represents an OUTER APPLY, which applies a table-valued function or derived table for each row from the left table. </summary>
        OuterApply,
        /// <summary> Represents a CROSS APPLY, which applies a table-valued function or derived table, filtering out unmatched rows. </summary>
        CrossApply,
        /// <summary> Represents a FULL OUTER JOIN, returning all records from both tables, with NULLs for unmatched rows. </summary>
        FullOuter
    }
}