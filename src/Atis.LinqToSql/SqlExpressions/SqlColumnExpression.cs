namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL column expression.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class can represent either a standard SQL column or a scalar column. A scalar column is a single column
    ///         selected during projection on a single field without using <c>NewExpression</c> or <c>MemberInitExpression</c>.
    ///     </para>
    /// </remarks>
    public class SqlColumnExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlColumnExpression"/> class.
        ///     </para>
        ///     <para>
        ///         The <paramref name="columnExpression"/> parameter specifies the column expression.
        ///     </para>
        ///     <para>
        ///         The <paramref name="columnAlias"/> parameter specifies the alias for the column.
        ///     </para>
        ///     <para>
        ///         The <paramref name="modelPath"/> parameter specifies the model path for the column.
        ///     </para>
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <param name="columnAlias">The alias of the column.</param>
        /// <param name="modelPath">The model path of the column.</param>
        public SqlColumnExpression(SqlExpression columnExpression, string columnAlias, ModelPath modelPath)
            : this(columnExpression, columnAlias, modelPath, scalar: false)
        {            
        }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlColumnExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <param name="columnAlias">The alias of the column.</param>
        /// <param name="modelPath">The model path of the column.</param>
        /// <param name="scalar">If <c>true</c> the column is a scalar column, otherwise it is a regular column.</param>
        public SqlColumnExpression(SqlExpression columnExpression, string columnAlias, ModelPath modelPath, bool scalar)
        {
            this.ColumnExpression = columnExpression;
            this.ColumnAlias = columnAlias;
            this.ModelPath = modelPath;
            if (scalar)
                this.NodeType = SqlExpressionType.ScalarColumn;
            else
                this.NodeType = SqlExpressionType.Column;
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Column"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType { get; }

        /// <summary>
        ///     <para>
        ///         Gets the column expression.
        ///     </para>
        /// </summary>
        public SqlExpression ColumnExpression { get; }

        /// <summary>
        ///     <para>
        ///         Gets the alias of the column.
        ///     </para>
        /// </summary>
        public string ColumnAlias { get; }

        /// <summary>
        ///     <para>
        ///         Gets the model path of the column.
        ///     </para>
        /// </summary>
        public ModelPath ModelPath { get; }

        /// <summary>
        ///     <para>
        ///         Updates the column expression, alias, and model path.
        ///     </para>
        ///     <para>
        ///         If the new values are the same as the current values, the current instance is returned.
        ///         Otherwise, a new instance with the updated values is returned.
        ///     </para>
        /// </summary>
        /// <param name="columnExpression">The new column expression.</param>
        /// <param name="columnAlias">The new alias of the column.</param>
        /// <param name="modelPath">The new model path of the column.</param>
        /// <returns>A new <see cref="SqlColumnExpression"/> instance with the updated values, or the current instance if unchanged.</returns>
        public SqlColumnExpression Update(SqlExpression columnExpression)
        {
            if (columnExpression == this.ColumnExpression)
                return this;
            return new SqlColumnExpression(columnExpression, this.ColumnAlias, this.ModelPath);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL column expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlColumnExpression(this);
        }

        // not updating the ModelPath, because we were copying projection elsewhere, so we experienced same
        // Projection expression was copied in a sql expression, then the source sql expression was
        // changing and it was causing the other sql expression's projection to be changed as well

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL column expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the column expression and its alias.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL column expression.</returns>
        public override string ToString()
        {
            return $"{this.ColumnExpression} as {this.ColumnAlias}";
        }
    }
}