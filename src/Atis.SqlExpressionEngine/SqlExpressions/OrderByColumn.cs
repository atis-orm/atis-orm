namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class OrderByColumn
    {
        public OrderByColumn(SqlExpression columnExpression, SortDirection direction)
        {
            this.ColumnExpression = columnExpression;
            this.Direction = direction;
        }

        public SqlExpression ColumnExpression { get; }
        public SortDirection Direction { get; }

        public override string ToString()
        {
            return $"{this.ColumnExpression} {(this.Direction == SortDirection.Ascending ? "asc" : "desc")}";
        }
    }
}