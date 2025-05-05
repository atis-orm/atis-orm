namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents an SQL alias expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define an alias for a column in an SQL query.
    ///     </para>
    /// </summary>
    public class SqlAliasExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Alias"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Alias;

        /// <summary>
        ///     Gets the alias of the column.
        /// </summary>
        public string ColumnAlias { get; }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlAliasExpression"/> class.
        ///     </para>
        ///     <para>
        ///         The <paramref name="columnAlias"/> parameter specifies the alias for the column.
        ///     </para>
        /// </summary>
        /// <param name="columnAlias">The alias of the column.</param>
        public SqlAliasExpression(string columnAlias)
        {
            this.ColumnAlias = columnAlias;
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL alias expression.
        ///     </para>
        ///     <para>
        ///         This method is used to implement the visitor pattern for SQL expressions.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlAlias(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL alias expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the alias of the column.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL alias expression.</returns>
        public override string ToString()
        {
            return $"{this.ColumnAlias}";
        }
    }
}