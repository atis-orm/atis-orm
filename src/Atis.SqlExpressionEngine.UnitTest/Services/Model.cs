using Atis.SqlExpressionEngine;
using Atis.SqlExpressionEngine.SqlExpressions;
using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Reflection;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.UnitTest.Services
{
    internal class Model : Atis.SqlExpressionEngine.Services.Model
    {
        public override IReadOnlyList<MemberInfo> GetColumnMembers(Type type)
        {
            return type.GetProperties()
                        .Where(x => x.GetCustomAttribute<NavigationPropertyAttribute>() == null &&
                                        x.GetCustomAttribute<CalculatedPropertyAttribute>() == null &&
                                        x.GetCustomAttribute<NavigationLinkAttribute>() == null)
                        .ToArray();
        }

        public override IReadOnlyList<MemberInfo> GetPrimaryKeys(Type type)
        {
            return this.GetColumnMembers(type)
                            .Where(x => x.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                            .ToArray();
        }

        public override IReadOnlyList<TableColumn> GetTableColumns(Type type)
        {
            return this.GetColumnMembers(type)
                            .Select(x => new TableColumn(x.Name, x.Name)).ToArray();
        }
    }
}
