using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// Represents the various operations that can be performed in a SQL query.
    /// </summary>
    public enum SqlQueryOperation
    {
        /// <summary>
        /// Represents a SELECT operation in a SQL query.
        /// </summary>
        Select,

        /// <summary>
        /// Represents a JOIN operation in a SQL query.
        /// </summary>
        Join,
        NavigationJoin,

        /// <summary>
        /// Represents a WHERE operation in a SQL query.
        /// </summary>
        Where,

        /// <summary>
        /// Represents a GROUP BY operation in a SQL query.
        /// </summary>
        GroupBy,

        /// <summary>
        /// Represents an ORDER BY operation in a SQL query.
        /// </summary>
        OrderBy,

        /// <summary>
        /// Represents a TOP operation in a SQL query.
        /// </summary>
        Top,

        /// <summary>
        /// Represents a DISTINCT operation in a SQL query.
        /// </summary>
        Distinct,

        /// <summary>
        /// Represents a ROW OFFSET operation in a SQL query.
        /// </summary>
        RowOffset,

        /// <summary>
        /// Represents a ROWS PER PAGE operation in a SQL query.
        /// </summary>
        RowsPerPage,
    }

}
