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
        ///     <para>
        ///         Gets an array of table columns corresponding to the specified type.
        ///     </para>
        /// </summary>
        /// <param name="type">The type of the model.</param>
        /// <returns>An array of <see cref="TableColumn"/> objects.</returns>
        TableColumn[] GetTableColumns(Type type);

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