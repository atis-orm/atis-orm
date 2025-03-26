using System;
using System.Collections.Generic;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a collection of different SQL Expression for special cases.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This expression is being used in <see cref="ExpressionExtensions.ChildJoinExpression"/>,
    ///         usually this can happen in the case of 2 Data Sources or multiple Columns are selected 
    ///         and in Join Expression we will be needing the source of those columns / data sources.
    ///     </para>
    ///     <para>
    ///         Caution: This class is just for communicating between converters and should not be used
    ///         in replacements.
    ///     </para>
    /// </remarks>
    public class SqlSelectedCollectionExpression : SqlCollectionExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlSelectedCollectionExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="sourceExpression">The source expression, usually data source.</param>
        /// <param name="sqlExpressions">The collection of SQL expressions, usually Data Sources or Column Expressions.</param>
        public SqlSelectedCollectionExpression(SqlExpression sourceExpression, IEnumerable<SqlExpression> sqlExpressions) : base(sqlExpressions)
        {
            this.SourceExpression = sourceExpression;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.SelectedCollection;

        /// <summary>
        ///     <para>
        ///         Gets the source expression, usually data source.
        ///     </para>
        /// </summary>
        public SqlExpression SourceExpression { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSelectedCollectionExpression(this);
        }
    }
}
