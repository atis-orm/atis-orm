namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SelectColumn
    {
        public SelectColumn(SqlExpression columnExpression, string alias, bool scalarColumn)
        {
            this.ColumnExpression = columnExpression;
            this.Alias = alias;
            this.ScalarColumn = scalarColumn;
        }

        public SqlExpression ColumnExpression { get; }
        public string Alias { get; }
        public bool ScalarColumn { get; }

        public override string ToString()
        {
            return $"{this.ColumnExpression} as {this.Alias}";
        }
    }
}
