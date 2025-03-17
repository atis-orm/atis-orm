using Atis.LinqToSql.Internal;
using System;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a reference to a Common Table Expression (CTE) in a SQL query.
    ///     </para>
    ///     <para>
    ///         This class is used to define and manage references to CTEs within SQL queries.
    ///     </para>
    /// </summary>
    public class SqlCteReferenceExpression : SqlQuerySourceExpression
    {
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.CteReference;

        /// <summary>
        ///     <para>
        ///         Gets the alias of the CTE.
        ///     </para>
        /// </summary>
        public Guid CteAlias { get; }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlCteReferenceExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Validates the CTE alias and sets it.
        ///     </para>
        /// </summary>
        /// <param name="cteAlias">The alias of the CTE.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cteAlias"/> is null.</exception>
        public SqlCteReferenceExpression()
        {
            this.CteAlias = Guid.NewGuid();// cteAlias ?? throw new ArgumentNullException(nameof(cteAlias));
        }

        public SqlCteReferenceExpression(Guid cteAlias)
        {
            this.CteAlias = cteAlias;
        }   

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the CTE reference expression.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the CTE reference expression.</returns>
        public override string ToString()
        {
            return $"cte: {DebugAliasGenerator.GetAlias(this.CteAlias)}";
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL CTE reference expression.
        ///     </para>
        /// </summary>
        /// <param name="visitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitCteReferenceExpression(this);
        }
    }
}
