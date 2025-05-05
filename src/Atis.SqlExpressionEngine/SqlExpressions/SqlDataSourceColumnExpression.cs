using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlDataSourceColumnExpression : SqlExpression, IEquatable<SqlDataSourceColumnExpression>
    {
        public SqlDataSourceColumnExpression(Guid dataSourceAlias, string columnName)
        {
            this.DataSourceAlias = dataSourceAlias;
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentNullException(nameof(columnName));
            this.ColumnName = columnName;
        }

        public override SqlExpressionType NodeType => SqlExpressionType.DataSourceColumn;
        public Guid DataSourceAlias { get; }
        public string ColumnName { get; }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlDataSourceColumn(this);
        }

        public override string ToString()
        {
            return $"{DebugAliasGenerator.GetAlias(this.DataSourceAlias)}.{this.ColumnName}";
        }

        public bool Equals(SqlDataSourceColumnExpression other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return this.DataSourceAlias == other.DataSourceAlias &&
                   this.ColumnName == other.ColumnName;
        }

        public override bool Equals(object obj) => Equals(obj as SqlDataSourceColumnExpression);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(this.DataSourceAlias);
            hash.Add(this.ColumnName);
            return hash.ToHashCode();
        }
    }
}
