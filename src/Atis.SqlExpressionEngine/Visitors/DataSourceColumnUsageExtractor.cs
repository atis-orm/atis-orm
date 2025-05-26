using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.Visitors
{
    public class DataSourceColumnUsageExtractor : SqlExpressionVisitor
    {
        private readonly HashSet<SqlDataSourceColumnExpression> dataSourceColumnUsages = new HashSet<SqlDataSourceColumnExpression>();
        private readonly HashSet<Guid> dataSourcesToSearch;
        private SqlExpression targetExpression;

        public DataSourceColumnUsageExtractor(HashSet<Guid> dataSourcesToSearch)
        {
            this.dataSourcesToSearch = dataSourcesToSearch ?? throw new ArgumentNullException(nameof(dataSourcesToSearch));
        }

        public static DataSourceColumnUsageExtractor FindDataSources(HashSet<Guid> dataSourcesToSearch)
        {
            return new DataSourceColumnUsageExtractor(dataSourcesToSearch);
        }

        public DataSourceColumnUsageExtractor In(SqlExpression sqlExpression)
        {
            if (sqlExpression is null)
                throw new ArgumentNullException(nameof(sqlExpression));
            this.targetExpression = sqlExpression;
            return this;
        }

        public IReadOnlyCollection<SqlDataSourceColumnExpression> ExtractDataSourceColumnExpressions()
        {
            if (this.targetExpression is null)
                throw new InvalidOperationException($"SqlExpression is not set, please call In method to set it.");
            this.dataSourceColumnUsages.Clear();
            this.Visit(this.targetExpression);
            return this.ConvertDataSourceColumnUsageToDataSourceColumnExpressions();
        }

        public static IReadOnlyCollection<SqlDataSourceColumnExpression> ExtractDataSourceColumnUsages(SqlExpression sqlExpression, HashSet<Guid> dataSourcesToSearch)
        {
            if (sqlExpression is null)
                throw new ArgumentNullException(nameof(sqlExpression));
            if (dataSourcesToSearch is null)
                throw new ArgumentNullException(nameof(dataSourcesToSearch));
            var extractor = new DataSourceColumnUsageExtractor(dataSourcesToSearch);
            extractor.Visit(sqlExpression);
            return extractor.ConvertDataSourceColumnUsageToDataSourceColumnExpressions();
        }

        private IReadOnlyCollection<SqlDataSourceColumnExpression> ConvertDataSourceColumnUsageToDataSourceColumnExpressions()
        {
            return this.dataSourceColumnUsages.ToArray();
        }

        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            if (this.dataSourcesToSearch.Contains(node.DataSourceAlias))
            {
                this.dataSourceColumnUsages.Add(node);
            }
            return base.VisitSqlDataSourceColumn(node);
        }
    }
}
