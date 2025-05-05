using System;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class ColumnModelPath : IEquatable<ColumnModelPath>
    {
        public ColumnModelPath(string columnName, ModelPath modelPath)
        {
            this.ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            this.ModelPath = modelPath;
        }

        public string ColumnName { get; }
        public ModelPath ModelPath { get; }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ColumnName);
        }

        public override bool Equals(object obj) => this.Equals(obj as ColumnModelPath);

        public bool Equals(ColumnModelPath other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return this.ColumnName == other.ColumnName;
        }
    }

    public class QueryableColumnModelPath : ColumnModelPath
    {
        public QueryableColumnModelPath(string columnName, ModelPath modelPath, SqlQueryableExpression queryable) : base(columnName, modelPath)
        {
            this.Queryable = queryable ?? throw new ArgumentNullException(nameof(queryable));
        }

        public SqlQueryableExpression Queryable { get; }
    }
}
