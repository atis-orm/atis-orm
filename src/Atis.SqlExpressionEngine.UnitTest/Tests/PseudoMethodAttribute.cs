namespace Atis.SqlExpressionEngine.UnitTest.Tests
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