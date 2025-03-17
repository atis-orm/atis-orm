using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NavigationPropertyAttribute : Attribute
    {
        public NavigationType NavigationType { get; }
        public Type RelationType { get; }

        public NavigationPropertyAttribute(NavigationType navigationType, Type relationType)
        {
            this.NavigationType = navigationType;
            this.RelationType = relationType;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NavigationLinkAttribute : Attribute
    {
        public string[] ParentKeys { get; }
        public string[] ForeignKeysInChild { get; }
        public NavigationType NavigationType { get; }

        public NavigationLinkAttribute(NavigationType navigationType, string parentKey, string foreignKeyInChild)
        {
            this.NavigationType = navigationType;
            this.ParentKeys = new string[] { parentKey };
            this.ForeignKeysInChild = new string[] { foreignKeyInChild };
        }

        public NavigationLinkAttribute(NavigationType navigationType, string[] parentKeys, string[] foreignKeysInChild)
        {
            this.ParentKeys = parentKeys;
            this.ForeignKeysInChild = foreignKeysInChild;
            this.NavigationType = navigationType;
        }
    }
}
