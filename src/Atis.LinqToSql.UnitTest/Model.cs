using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{
    internal class Model : ContextExtensions.Model
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
