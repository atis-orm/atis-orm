using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Atis.SqlExpressionEngine.Services
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
        public virtual IReadOnlyList<MemberInfo> GetPrimaryKeys(Type type)
        {
            return type.GetProperties();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<MemberInfo> GetColumnMembers(Type type)
        {
            return type.GetProperties().ToList();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<TableColumn> GetTableColumns(Type type)
        {
            return type.GetProperties().Select(x => new TableColumn(x.Name, x.Name)).ToArray();
        }

        /// <inheritdoc />
        public virtual SqlTable GetSqlTable(Type type)
        {
            return new SqlTable(type.Name);
        }
    }
}
