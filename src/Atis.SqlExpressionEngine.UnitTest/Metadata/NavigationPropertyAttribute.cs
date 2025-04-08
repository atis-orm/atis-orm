namespace Atis.SqlExpressionEngine.UnitTest.Metadata
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
}
