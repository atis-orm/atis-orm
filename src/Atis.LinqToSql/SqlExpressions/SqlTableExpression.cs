using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL table expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define a table in a SQL query.
    ///     </para>
    /// </summary>
    public class SqlTableExpression : SqlQuerySourceExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlTableExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the table name and columns.
        ///     </para>
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="tableColumns">The columns of the table.</param>
        /// <exception cref="ArgumentNullException">
        ///     <para>
        ///         Thrown when the <paramref name="tableName"/> or <paramref name="tableColumns"/> is null.
        ///     </para>
        /// </exception>
        public SqlTableExpression(string tableName, TableColumn[] tableColumns)
        {
            this.TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            this.TableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
            this.propertyMap = tableColumns.ToDictionary(x => x.ModelPropertyName, x => x.DatabaseColumnName);
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Table"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Table;

        /// <summary>
        ///     <para>
        ///         Gets the name of the table.
        ///     </para>
        /// </summary>
        public string TableName { get; }

        /// <summary>
        ///     <para>
        ///         Gets the columns of the table.
        ///     </para>
        /// </summary>
        public TableColumn[] TableColumns { get; }

        private readonly Dictionary<string, string> propertyMap;

        /// <summary>
        ///     <para>
        ///         Gets the database column name by the model property name.
        ///     </para>
        /// </summary>
        /// <param name="propertyName">The name of the model property.</param>
        /// <returns>The name of the database column.</returns>
        /// <exception cref="InvalidOperationException">
        ///     <para>
        ///         Thrown when the <paramref name="propertyName"/> is not found in the table.
        ///     </para>
        /// </exception>
        public string GetByPropertyName(string propertyName)
        {
            if (this.propertyMap.TryGetValue(propertyName, out var columnName))
                return columnName;
            throw new InvalidOperationException($"Property '{propertyName}' not found in table '{this.TableName}'.");
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL table expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlTableExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL table expression.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL table expression.</returns>
        public override string ToString()
        {
            return this.TableName;
        }
    }
}
