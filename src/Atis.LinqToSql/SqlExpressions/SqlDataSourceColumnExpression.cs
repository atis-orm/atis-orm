using Atis.LinqToSql.Internal;
using System;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a column in a data source with an alias.
    ///     </para>
    ///     <para>
    ///         This class is used to define a column in a data source with an alias and column name.
    ///     </para>
    /// </summary>
    public class SqlDataSourceColumnExpression : SqlExpression
    {
        public SqlDataSourceColumnExpression(SqlDataSourceExpression dataSource, string columnName)
        {
            this.DataSource = dataSource;
            this.ColumnName = columnName;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.DataSourceColumn;

        // this will not be visited, this is just for reference
        public SqlDataSourceExpression DataSource { get; }

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
            return $"{DebugAliasGenerator.GetAlias(this.DataSource)}.{this.ColumnName}";
        }
    }
}
