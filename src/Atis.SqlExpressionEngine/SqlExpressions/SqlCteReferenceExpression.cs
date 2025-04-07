using Atis.SqlExpressionEngine.Internal;
using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a reference to a Common Table Expression (CTE) in a SQL query.
    ///     </para>
    ///     <para>
    ///         The CTE itself is added as a sub-query data source in the <see cref="SqlQueryExpression"/>.
    ///         When referencing the CTE, this expression is used.
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
        ///         Creates a new instance of the <see cref="SqlCteReferenceExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="cteAlias">Alias of the CTE.</param>
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
