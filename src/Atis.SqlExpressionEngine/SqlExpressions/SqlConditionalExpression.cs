namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a conditional SQL expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define conditional logic in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlConditionalExpression : SqlExpression
    {
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Conditional;

        /// <summary>
        ///     <para>
        ///         Gets the test expression of the conditional expression.
        ///     </para>
        /// </summary>
        public SqlExpression Test { get; }

        /// <summary>
        ///     <para>
        ///         Gets the expression to evaluate if the test expression is true.
        ///     </para>
        /// </summary>
        public SqlExpression IfTrue { get; }

        /// <summary>
        ///     <para>
        ///         Gets the expression to evaluate if the test expression is false.
        ///     </para>
        /// </summary>
        public SqlExpression IfFalse { get; }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlConditionalExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the test, if-true, and if-false expressions.
        ///     </para>
        /// </summary>
        /// <param name="test">The test expression.</param>
        /// <param name="ifTrue">The expression to evaluate if the test expression is true.</param>
        /// <param name="ifFalse">The expression to evaluate if the test expression is false.</param>
        public SqlConditionalExpression(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse)
        {
            Test = test;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL conditional expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlConditional(this);
        }

        /// <summary>
        ///     <para>
        ///         Updates the conditional expression with new test, if-true, and if-false expressions.
        ///     </para>
        ///     <para>
        ///         If the new expressions are the same as the current expressions, the current instance is returned.
        ///         Otherwise, a new instance with the updated expressions is returned.
        ///     </para>
        /// </summary>
        /// <param name="test">The new test expression.</param>
        /// <param name="ifTrue">The new if-true expression.</param>
        /// <param name="ifFalse">The new if-false expression.</param>
        /// <returns>A new <see cref="SqlConditionalExpression"/> instance with the updated expressions, or the current instance if unchanged.</returns>
        public SqlConditionalExpression Update(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse)
        {
            if (test == Test && ifTrue == IfTrue && ifFalse == IfFalse)
                return this;
            return new SqlConditionalExpression(test, ifTrue, ifFalse);
        }
    }
}
