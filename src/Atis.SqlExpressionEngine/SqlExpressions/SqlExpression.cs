using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// Represents different types of SQL expressions in the query expression tree.
    /// </summary>
    public enum SqlExpressionType
    {
        /// <summary> A custom SQL expression type that does not fit predefined categories. </summary>
        Custom,
        /// <summary> Represents an addition operation in an SQL expression. </summary>
        Add,
        /// <summary> Represents a subtraction operation in an SQL expression. </summary>
        Subtract,
        /// <summary> Represents a multiplication operation in an SQL expression. </summary>
        Multiply,
        /// <summary> Represents a division operation in an SQL expression. </summary>
        Divide,
        /// <summary> Represents a logical AND operation (`AND` in SQL). </summary>
        AndAlso,
        /// <summary> Represents a logical OR operation (`OR` in SQL). </summary>
        OrElse,
        /// <summary> Represents a less-than comparison (`<` in SQL). </summary>
        LessThan,
        /// <summary> Represents a less-than-or-equal comparison (`<=` in SQL). </summary>
        LessThanOrEqual,
        /// <summary> Represents a greater-than comparison (`>` in SQL). </summary>
        GreaterThan,
        /// <summary> Represents a greater-than-or-equal comparison (`>=` in SQL). </summary>
        GreaterThanOrEqual,
        /// <summary> Represents an equality comparison (`=` in SQL). </summary>
        Equal,
        /// <summary> Represents a not-equal comparison (`!=` or `<>` in SQL). </summary>
        NotEqual,
        /// <summary> Represents a LIKE comparison for pattern matching. </summary>
        Like,
        /// <summary> Represents a modulus (remainder) operation (`%` in SQL). </summary>
        Modulus,
        /// <summary> Represents a projection in an SQL query (SELECT clause). </summary>
        Projection,
        /// <summary> Represents a column reference in an SQL expression. </summary>
        Column,
        /// <summary> Represents a data source column (column directly from a table or view). </summary>
        DataSourceColumn,
        /// <summary> Represents a table reference in an SQL query. </summary>
        Table,
        /// <summary> Represents an entire SQL query as an expression. </summary>
        Query,
        /// <summary> Represents a JOIN operation between tables or queries. </summary>
        Join,
        /// <summary> Represents a general data source, such as a table or subquery. </summary>
        DataSource,
        /// <summary> Represents a parameterized value in an SQL query. </summary>
        Parameter,
        /// <summary> Represents a function call (e.g., `COUNT()`, `SUM()`). </summary>
        FunctionCall,
        /// <summary> Represents a raw SQL fragment included in a query. </summary>
        Fragment,
        /// <summary> Represents a literal value (e.g., numbers, strings). </summary>
        Literal,
        /// <summary> Represents a table as a data source. </summary>
        TableDataSource,
        /// <summary> Represents a subquery used as a data source. </summary>
        SubQueryDataSource,
        /// <summary> Represents a scalar column expression, often used in projections. </summary>
        ScalarColumn,
        /// <summary> Represents an EXISTS clause in an SQL query. </summary>
        Exists,
        /// <summary> Represents an ORDER BY clause in an SQL query. </summary>
        OrderBy,
        /// <summary> Represents a collection of SQL expressions, often used for IN clauses. </summary>
        Collection,
        /// <summary> Represents an alias for another SQL expression (e.g., column aliases). </summary>
        Alias,
        /// <summary> Represents a placeholder or dummy expression, often used for debugging. </summary>
        Dummy,
        /// <summary> Represents a FROM source in an SQL query. </summary>
        FromSource,
        /// <summary> Represents a reference to a previously defined data source. </summary>
        DataSourceReference,
        /// <summary> Represents a UNION operation in an SQL query. </summary>
        Union,
        /// <summary> Represents a UNION ALL operation in an SQL query. </summary>
        UnionAll,
        /// <summary> Represents a reference to a Common Table Expression (CTE). </summary>
        CteReference,
        /// <summary> Represents a selected collection of columns or expressions in an SQL query. </summary>
        SelectedCollection,
        /// <summary> Represents a data source that has been joined to another. </summary>
        JoinedDataSource,
        /// <summary> Represents the SQL `OUTER APPLY` or `LEFT JOIN ... ON TRUE` when no matching records exist. </summary>
        DefaultIfEmpty,
        /// <summary> Represents the COALESCE function, which returns the first non-null value. </summary>
        Coalesce,
        /// <summary> Represents a CASE WHEN conditional expression in SQL. </summary>
        Conditional,
        Update,
        Delete,
        Not,
        CteDataSource,
        SubQueryColumn,
        In,
        Keyword,
        Negate,
        Cast,
        DateAdd,
        DatePart,
        StringFunction,
        LikeStartsWith,
        LikeEndsWith
    }


    /// <summary>
    ///     <para>
    ///         Represents a base class for SQL expressions.
    ///     </para>
    /// </summary>
    public abstract class SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Gets the unique identifier for the SQL expression.
        ///     </para>
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        /// </summary>
        public virtual SqlExpressionType NodeType { get; } = SqlExpressionType.Custom;

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal virtual SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitCustom(this);
        }

        protected internal virtual SqlExpression VisitChildren(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return this;
        }
    }
}
