using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Atis.SqlExpressionEngine.Abstractions
{
    /// <summary>
    ///     <para>
    ///         Interface representing a model that provides methods to retrieve table and column information.
    ///     </para>
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Gets the list of members from type that represent database columns.
        /// </summary>
        /// <param name="type">Type of table source.</param>
        /// <returns>List of members representing columns.</returns>
        IReadOnlyList<MemberInfo> GetColumnMembers(Type type);

        /// <summary>
        /// Gets the first primary key member of the specified type.
        /// </summary>
        /// <param name="type">Type of the table source.</param>
        /// <returns>List of primary key members.</returns>
        IReadOnlyList<MemberInfo> GetPrimaryKeys(Type type);

        /// <summary>
        ///     <para>
        ///         Gets a list of table columns corresponding to the specified type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type of the model.</param>
        /// <returns>A list of <see cref="TableColumn"/> objects.</returns>
        IReadOnlyList<TableColumn> GetTableColumns(Type type);

        /// <summary>
        ///     <para>
        ///         Gets the name of the table corresponding to the specified type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type of the model.</param>
        /// <returns>The name of the table.</returns>
        string GetTableName(Type type);
    }
}