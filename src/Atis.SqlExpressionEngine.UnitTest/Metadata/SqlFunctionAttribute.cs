namespace Atis.SqlExpressionEngine.UnitTest.Metadata
{
    public class SqlFunctionAttribute : Attribute
    {
        public string SqlFunctionName { get; }

        public SqlFunctionAttribute(string sqlFunctionName)
        {
            this.SqlFunctionName = sqlFunctionName;
        }
    }
}
