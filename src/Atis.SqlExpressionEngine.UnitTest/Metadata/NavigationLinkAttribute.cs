namespace Atis.SqlExpressionEngine.UnitTest.Metadata
{
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
