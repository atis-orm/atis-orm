using System;
using System.Collections.Generic;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL UNION expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define UNION and UNION ALL operations in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlUnionExpression : SqlExpression
    {
        private static readonly ISet<SqlExpressionType> _allowedTypes = new HashSet<SqlExpressionType>
            {
                SqlExpressionType.Union,
                SqlExpressionType.UnionAll,
            };

        private static SqlExpressionType ValidateNodeType(SqlExpressionType nodeType)
            => _allowedTypes.Contains(nodeType)
                ? nodeType
                : throw new InvalidOperationException($"SqlExpressionType '{nodeType}' is not a valid Union Type");

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlUnionExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Validates the node type and sets the query expression.
        ///     </para>
        /// </summary>
        /// <param name="query">The SQL query expression.</param>
        /// <param name="sqlExpressionType">The type of the SQL UNION operation.</param>
        public SqlUnionExpression(SqlQueryExpression query, SqlExpressionType sqlExpressionType)
        {
            this.Query = query;
            this.NodeType = ValidateNodeType(sqlExpressionType);
        }

        /// <summary>
        ///     <para>
        ///         Gets the SQL query expression.
        ///     </para>
        /// </summary>
        public SqlQueryExpression Query { get; }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType { get; }

        /// <summary>
        ///     <para>
        ///         Updates the SQL UNION expression with new query and node type.
        ///     </para>
        ///     <para>
        ///         If the new values are the same as the current values, the current instance is returned.
        ///         Otherwise, a new instance with the updated values is returned.
        ///     </para>
        /// </summary>
        /// <param name="query">The new SQL query expression.</param>
        /// <param name="nodeType">The new node type.</param>
        /// <returns>A new <see cref="SqlUnionExpression"/> instance with the updated values, or the current instance if unchanged.</returns>
        public SqlUnionExpression Update(SqlQueryExpression query)
        {
            if (this.Query == query)
                return this;
            return new SqlUnionExpression(query, this.NodeType);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL UNION expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlUnionExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL UNION expression.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL UNION expression.</returns>
        public override string ToString()
        {
            return $"{this.NodeType}\r\n{this.Query}";
        }
    }
}
