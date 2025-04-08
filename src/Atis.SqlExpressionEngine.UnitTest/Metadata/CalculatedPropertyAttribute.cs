namespace Atis.SqlExpressionEngine.UnitTest.Metadata
{
    public class CalculatedPropertyAttribute : Attribute
    {
        public string ExpressionPropertyName { get; set; }
        public CalculatedPropertyAttribute(string expressionPropertyName)
        {
            this.ExpressionPropertyName = expressionPropertyName;
        }
    }
}
