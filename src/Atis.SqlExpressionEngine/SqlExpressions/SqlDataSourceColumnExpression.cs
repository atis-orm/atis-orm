using Atis.SqlExpressionEngine.Internal;
using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a column in a data source with an alias.
    ///     </para>
    /// </summary>
    public class SqlDataSourceColumnExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Creates a new instance of the <see cref="SqlDataSourceColumnExpression" /> class.
        ///     </para>
        /// </summary>
        /// <param name="dataSource">Instance of <see cref="SqlDataSourceExpression"/> class.</param>
        /// <param name="columnName">Column name in the data source.</param>
        public SqlDataSourceColumnExpression(SqlDataSourceExpression dataSource, string columnName)
        {
            this.DataSourceReference = new SqlDataSourceReferenceExpression(dataSource);
            this.ColumnName = columnName;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.DataSourceColumn;

        /// <summary>
        ///     <para>
        ///         Gets the data source of the column.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This property will not be visited by the <see cref="SqlExpressionVisitor" />.
        ///         This is just for the reference of the data source.
        ///     </para>
        /// </remarks>
        public SqlDataSourceReferenceExpression DataSourceReference { get; }

        /// <summary>
        ///     <para>
        ///         Gets the name of the column.
        ///     </para>
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL data source column expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlDataSourceColumnExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL data source column expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the alias of the data source and the name of the column.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL data source column expression.</returns>
        public override string ToString()
        {
            return $"{DebugAliasGenerator.GetAlias(this.DataSourceReference.Reference.DataSourceAlias)}.{this.ColumnName}";
        }
    }
}
