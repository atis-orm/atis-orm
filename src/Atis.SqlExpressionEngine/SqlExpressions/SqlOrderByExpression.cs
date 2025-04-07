namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents an SQL ORDER BY expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define the ORDER BY clause in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlOrderByExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlOrderByExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the SQL expression and sort order for the ORDER BY clause.
        ///     </para>
        /// </summary>
        /// <param name="expression">The SQL expression to be used in the ORDER BY clause.</param>
        /// <param name="ascending">A value indicating whether the sort order is ascending.</param>
        public SqlOrderByExpression(SqlExpression expression, bool ascending)
        {
            this.Ascending = ascending;
            this.Expression = expression;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.OrderBy;

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether the sort order is ascending.
        ///     </para>
        /// </summary>
        public bool Ascending { get; }
        /// <summary>
        ///     <para>
        ///         Gets the SQL expression to be used in the ORDER BY clause.
        ///     </para>
        /// </summary>
        public SqlExpression Expression { get; }

        
        public SqlOrderByExpression Update(SqlExpression expression)
        {
            if (expression == this.Expression)
                return this;
            return new SqlOrderByExpression(expression, this.Ascending);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL ORDER BY expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlOrderByExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL ORDER BY expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the expression and the sort order.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL ORDER BY expression.</returns>
        public override string ToString()
        {
            return $"{this.Expression} {(this.Ascending ? "asc" : "desc")}";
        }
    }
}
