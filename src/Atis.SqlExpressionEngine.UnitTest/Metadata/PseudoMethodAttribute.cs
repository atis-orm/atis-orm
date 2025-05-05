namespace Atis.SqlExpressionEngine.UnitTest.Metadata
{
    public class PseudoMethodAttribute : Attribute
    {
        public PseudoMethodAttribute(string expressionProperty)
        {
            this.ExpressionProperty = expressionProperty;
        }

        public string ExpressionProperty { get; }

    }
}