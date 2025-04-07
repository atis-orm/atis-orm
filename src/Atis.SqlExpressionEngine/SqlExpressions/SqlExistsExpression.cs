using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL EXISTS expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define an EXISTS clause in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlExistsExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlExistsExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the SQL query and applies the projection for EXISTS.
        ///     </para>
        /// </summary>
        /// <param name="sqlQuery">The SQL query expression.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="sqlQuery"/> is null.</exception>
        public SqlExistsExpression(SqlQueryExpression sqlQuery)
        {
            this.SqlQuery = sqlQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
            this.SqlQuery.SetProjectionForExists();
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Exists"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Exists;

        /// <summary>
        ///     <para>
        ///         Gets the SQL query expression.
        ///     </para>
        /// </summary>
        public SqlQueryExpression SqlQuery { get; }

        /// <summary>
        ///     <para>
        ///         Updates the SQL EXISTS expression with a new subquery.
        ///     </para>
        ///     <para>
        ///         If the new subquery is the same as the current subquery, the current instance is returned.
        ///         Otherwise, a new instance with the updated subquery is returned.
        ///     </para>
        /// </summary>
        /// <param name="subQuery">The new subquery expression.</param>
        /// <returns>A new <see cref="SqlExistsExpression"/> instance with the updated subquery, or the current instance if unchanged.</returns>
        public SqlExistsExpression Update(SqlQueryExpression subQuery)
        {
            if (subQuery == this.SqlQuery)
                return this;
            return new SqlExistsExpression(subQuery);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL EXISTS expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlExistsExpression(this);
        }
    }
}
