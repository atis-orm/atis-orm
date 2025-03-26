using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql.SqlExpressions
{
    public class SqlSubQueryColumnExpression : SqlColumnExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.SubQueryColumn;

        // will not visit in this property because it's a reference
        public SqlQueryExpression SubQuery { get; }

        public SqlSubQueryColumnExpression(SqlExpression columnExpression, string columnAlias, ModelPath modelPath, SqlQueryExpression subQuery) : base(columnExpression, columnAlias, modelPath)
        {
            this.SubQuery = subQuery;
        }
    }
}
