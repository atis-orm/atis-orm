using System;

namespace Atis.LinqToSql
{
    public interface IConversionContext
    {
        void AddExtension(object contextExtension);
        object GetExtension(Type extensionType);
        T GetExtension<T>() where T : class;
        T GetExtensionRequired<T>() where T : class;
    }
}