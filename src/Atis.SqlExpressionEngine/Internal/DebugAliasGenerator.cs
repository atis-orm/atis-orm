using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Internal
{
    /// <summary>
    ///     <para>
    ///         Provides a mechanism for generating human-readable aliases for GUIDs 
    ///         within the same thread, making debugging easier.
    ///     </para>
    ///     <para>
    ///         Each thread maintains its own alias mappings, ensuring that the same GUID 
    ///         within a thread always maps to the same alias, while different threads 
    ///         have independent mappings.
    ///     </para>
    /// </summary>
    public class DebugAliasGenerator
    {
        private readonly Dictionary<Guid, string> aliases = new Dictionary<Guid, string>();

        /// <summary>
        ///     <para>
        ///         Retrieves or generates an alias for the given GUID. If the GUID has already been assigned 
        ///         an alias in the current thread, the same alias is returned. Otherwise, a new alias is generated.
        ///     </para>
        /// </summary>
        /// <param name="uniqueId">The GUID for which an alias is required.</param>
        /// <param name="prefix"></param>
        /// <returns>A human-readable alias corresponding to the provided GUID.</returns>
        public string GetAliasName(Guid uniqueId, string prefix = "t")
        {
            if (!this.aliases.TryGetValue(uniqueId, out var alias))
            {
                this.aliasCount++;
                alias = this.GenerateAlias(aliasCount, prefix);
                this.aliases[uniqueId] = alias;   
            }
            return alias;
        }

        private int aliasCount = 0;

        private string GenerateAlias(int aliasNumber, string prefix = "t")
        {
            return $"{prefix}_{aliasNumber}";
        }

        [ThreadStatic]
        private static DebugAliasGenerator instance;

        /// <summary>
        ///     <para>
        ///         Retrieves a human-readable alias for the provided GUID. This ensures that the same GUID 
        ///         within a thread always returns the same alias while different threads maintain independent mappings.
        ///     </para>
        ///     <para>
        ///         If the instance of <see cref="DebugAliasGenerator"/> is not initialized for the current thread, 
        ///         it is created before generating the alias.
        ///     </para>
        /// </summary>
        /// <param name="uniqueId">The GUID for which a readable alias is required.</param>
        /// <param name="prefix"></param>
        /// <returns>A human-readable alias for the specified GUID.</returns>
        public static string GetAlias(Guid uniqueId, string prefix = "t")
        {
            if (instance is null)
                instance = new DebugAliasGenerator();
            return instance.GetAliasName(uniqueId, prefix);
        }

        public static string GetAlias(SqlAliasedDataSourceExpression dataSource)
        {
            if (dataSource is null)
                throw new ArgumentNullException(nameof(dataSource));
            var prefix = "t";
            if(dataSource is SqlAliasedJoinSourceExpression joinSource)
            {
                prefix = joinSource.JoinName ?? prefix;
            }
            return GetAlias(dataSource.Alias, prefix);
        }
    }

}
