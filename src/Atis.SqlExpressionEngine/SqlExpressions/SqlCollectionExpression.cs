using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{

    /// <summary>
    ///     <para>
    ///         Represents a collection of SQL expressions.
    ///     </para>
    ///     <para>
    ///         This class is used to define a collection of SQL expressions in a query.
    ///     </para>
    /// </summary>
    public class SqlCollectionExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Collection"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Collection;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlCollectionExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the collection of SQL expressions.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressions">The collection of SQL expressions.</param>
        public SqlCollectionExpression(IEnumerable<SqlExpression> sqlExpressions)
        {
            this.SqlExpressions = sqlExpressions;
        }

        /// <summary>
        ///     <para>
        ///         Gets the collection of SQL expressions.
        ///     </para>
        /// </summary>
        public IEnumerable<SqlExpression> SqlExpressions { get; }

        /// <summary>
        ///     <para>
        ///         Updates the collection of SQL expressions.
        ///     </para>
        ///     <para>
        ///         If the new collection is the same as the current collection, the current instance is returned.
        ///         Otherwise, a new instance with the updated collection is returned.
        ///     </para>
        /// </summary>
        /// <param name="items">The new collection of SQL expressions.</param>
        /// <returns>A new <see cref="SqlCollectionExpression"/> instance with the updated collection, or the current instance if the collection is unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="items"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="items"/> is empty.</exception>
        public SqlCollectionExpression Update(IEnumerable<SqlExpression> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (!items.Any())
                throw new ArgumentException("items is empty.", nameof(items));
            // compare each item in items with this.SqlExpressions and if any item is different, return a new SqlCollectionExpression
            if (items.SequenceEqual(this.SqlExpressions))
                return this;
            return new SqlCollectionExpression(items);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL collection expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlCollection(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the collection of SQL expressions.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the collection of SQL expressions.</returns>
        public override string ToString()
        {
            return $"collection: [{string.Join(", ", this.SqlExpressions.Select(x => x.ToString()))}]";
        }
    }
}
