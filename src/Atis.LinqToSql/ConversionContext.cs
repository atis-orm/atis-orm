using Atis.LinqToSql.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql
{
    /// <summary>
    ///     <para>
    ///         Conversion context for single query expression converter process.
    ///     </para>
    /// </summary>
    public class ConversionContext : IConversionContext
    {
        private readonly HashSet<object> contextExtensions = new HashSet<object>();

        public ConversionContext(IEnumerable<object> extensions)
        {
            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    this.contextExtensions.Add(extension);
                }
            }
        }

        public void AddExtension(object contextExtension)
        {
            this.contextExtensions.Add(contextExtension);
        }

        public object GetExtension(Type extensionType)
        {
            return this.contextExtensions.FirstOrDefault(x => x.GetType() == extensionType);
        }

        public T GetExtension<T>() where T : class
        {
            return this.contextExtensions.OfType<T>().FirstOrDefault();
        }

        public T GetExtensionRequired<T>() where T : class
        {
            var value = this.GetExtension<T>();
            if (value == null)
                throw new InvalidOperationException($"Extension of type {typeof(T).Name} is not found");
            return value;
        }
    }
}
