namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents different types of SQL joins used in queries.
    ///     </para>
    /// </summary>
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


    /// <summary>
    ///     <para>
    ///         Represents a SQL join expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define join operations in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlJoinExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlJoinExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the join type, joined source, and join condition.
        ///     </para>
        /// </summary>
        /// <param name="joinType">The type of the join operation.</param>
        /// <param name="joinedSource">The data source to join.</param>
        /// <param name="joinCondition">The condition for the join.</param>
        public SqlJoinExpression(SqlJoinType joinType, SqlDataSourceExpression joinedSource, SqlExpression joinCondition)
        {
            this.JoinType = joinType;
            this.JoinedSource = joinedSource;
            this.JoinCondition = joinCondition;
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Join"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Join;

        /// <summary>
        ///     <para>
        ///         Gets the type of the join operation.
        ///     </para>
        /// </summary>
        public SqlJoinType JoinType { get; private set; }

        /// <summary>
        ///     <para>
        ///         Gets the data source to join.
        ///     </para>
        /// </summary>
        public SqlDataSourceExpression JoinedSource { get; }

        /// <summary>
        ///     <para>
        ///         Gets the condition for the join.
        ///     </para>
        /// </summary>
        public SqlExpression JoinCondition { get; }

        public SqlJoinExpression Update(SqlDataSourceExpression joinedSource, SqlExpression joinCondition)
        {
            if (joinedSource == this.JoinedSource && joinCondition == this.JoinCondition)
                return this;
            return new SqlJoinExpression(this.JoinType, joinedSource, joinCondition);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL join expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlJoinExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Updates the type of the join operation.
        ///     </para>
        /// </summary>
        /// <param name="joinType">The new type of the join operation.</param>
        public void UpdateJoinType(SqlJoinType joinType)
        {
            this.JoinType = joinType;
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL join expression.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL join expression.</returns>
        public override string ToString()
        {
            var condition = this.JoinCondition != null ? $"on {this.JoinCondition}" : "";
            string joinType;
            switch (this.JoinType)
            {
                case SqlJoinType.Left:
                    joinType = "left join";
                    break;
                case SqlJoinType.Right:
                    joinType = "right join";
                    break;
                case SqlJoinType.Inner:
                    joinType = "inner join";
                    break;
                case SqlJoinType.Cross:
                    joinType = "cross join";
                    break;
                case SqlJoinType.OuterApply:
                    joinType = "outer apply";
                    break;
                case SqlJoinType.CrossApply:
                    joinType = "cross apply";
                    break;
                default:
                    joinType = this.JoinType.ToString();
                    break;
            }
            return $"{joinType} {this.JoinedSource}{condition}";
        }
    }
}
