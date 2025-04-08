using Atis.SqlExpressionEngine;
using Atis.SqlExpressionEngine.SqlExpressions;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Reflection;

namespace Atis.SqlExpressionEngine.UnitTest.Services
{
    internal class Model : Atis.SqlExpressionEngine.Services.Model
    {
        public override TableColumn[] GetTableColumns(Type type)
        {
            return type.GetProperties()
                            .Where(x => x.GetCustomAttribute<NavigationPropertyAttribute>() == null && 
                                            x.GetCustomAttribute<CalculatedPropertyAttribute>() == null &&
                                            x.GetCustomAttribute<NavigationLinkAttribute>() == null)
                            .Select(x => new TableColumn(x.Name, x.Name)).ToArray();
        }
    }
}
