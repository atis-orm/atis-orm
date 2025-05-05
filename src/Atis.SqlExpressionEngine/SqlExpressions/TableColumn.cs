namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class TableColumn
    {
        public TableColumn(string databaseColumnName, string modelPropertyName)
        {
            this.DatabaseColumnName = databaseColumnName;
            this.ModelPropertyName = modelPropertyName;
        }

        public string DatabaseColumnName { get; }
        public string ModelPropertyName { get; }
    }
}
