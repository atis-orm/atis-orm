using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Atis.LinqToSql.Services
{
    /// <summary>
    ///     <para>
    ///         Default implementation of <see cref="IModel"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class simply assumes that all the properties in given type as columns.
    ///     </para>
    ///     <para>
    ///         Similarly, it assumes that the table name is the same as the type name.
    ///     </para>
    /// </remarks>
    public class Model : IModel
    {
        /// <inheritdoc />
        public virtual TableColumn[] GetTableColumns(Type type)
        {
            return type.GetProperties().Select(x => new TableColumn(x.Name, x.Name)).ToArray();
        }

        /// <inheritdoc />
        public virtual string GetTableName(Type type)
        {
            return type.Name;
        }
    }
}
