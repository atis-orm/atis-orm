using System;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlDeleteExpression : SqlExpression
    {
        public SqlDeleteExpression(SqlDerivedTableExpression source, Guid dataSourceAlias)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.DataSourceAlias = dataSourceAlias;
            var ds = this.Source.AllDataSources.Where(x => x.Alias == dataSourceAlias).FirstOrDefault()
                ??
                throw new ArgumentException("The deleting data source must be part of the query.", nameof(dataSourceAlias));
            if (!(ds.QuerySource is SqlTableExpression))
                throw new ArgumentException("The deleting data source must be a table.", nameof(dataSourceAlias));
        }

        public SqlDerivedTableExpression Source { get; }
        public Guid DataSourceAlias { get; }
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Delete;

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlDelete(this);
        }

        public SqlExpression Update(SqlDerivedTableExpression source)
        {
            if (this.Source == source)
            {
                return this;
            }
            return new SqlDeleteExpression(source, this.DataSourceAlias);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"delete {this.DataSourceAlias}\r\n{this.Source}";
        }
    }
}
