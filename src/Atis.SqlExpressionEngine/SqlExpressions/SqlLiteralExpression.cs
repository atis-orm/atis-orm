namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL literal expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define a literal value in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlLiteralExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlLiteralExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the literal value of the expression.
        ///     </para>
        /// </summary>
        /// <param name="literalValue">The literal value of the expression.</param>
        public SqlLiteralExpression(object literalValue)
        {
            this.LiteralValue = literalValue;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Literal;

        /// <summary>
        ///     <para>
        ///         Gets the literal value of the expression.
        ///     </para>
        /// </summary>
        public object LiteralValue { get; }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL literal expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlLiteral(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL literal expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the literal value.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL literal expression.</returns>
        public override string ToString()
        {
            return SqlParameterExpression.ConvertObjectToString(this.LiteralValue);
        }
    }
}
