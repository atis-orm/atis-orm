namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlExpressionBinding
    {
        public SqlExpressionBinding(SqlExpression sqlExpression, ModelPath modelPath)
        {
            this.SqlExpression = sqlExpression;
            this.ModelPath = modelPath;
        }

        public SqlExpression SqlExpression { get; }
        public ModelPath ModelPath { get; set; }
    }
    public class NonProjectableBinding : SqlExpressionBinding
    {
        public NonProjectableBinding(SqlExpression sqlExpression, ModelPath modelPath) : base(sqlExpression, modelPath)
        {
        }
    }
}
