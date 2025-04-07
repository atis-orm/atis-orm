namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a column in a data source with an alias.
    ///     </para>
    ///     <para>
    ///         This class is used to define a column in a data source with an alias and column name.
    ///     </para>
    /// </summary>
    public class TableColumn
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="TableColumn"/> class.
        ///     </para>
        ///     <para>
        ///         The <paramref name="databaseColumnName"/> parameter specifies the name of the database column.
        ///     </para>
        ///     <para>
        ///         The <paramref name="modelPropertyName"/> parameter specifies the name of the model property.
        ///     </para>
        /// </summary>
        /// <param name="databaseColumnName">The name of the database column.</param>
        /// <param name="modelPropertyName">The name of the model property.</param>
        public TableColumn(string databaseColumnName, string modelPropertyName)
        {
            this.DatabaseColumnName = databaseColumnName;
            this.ModelPropertyName = modelPropertyName;
        }

        /// <summary>
        ///     <para>
        ///         Gets the name of the database column.
        ///     </para>
        /// </summary>
        public string DatabaseColumnName { get; }

        /// <summary>
        ///     <para>
        ///         Gets the name of the model property.
        ///     </para>
        /// </summary>
        public string ModelPropertyName { get; }
    }
}
