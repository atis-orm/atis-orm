using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql.SqlExpressions
{
    public class SqlOuterApplyQueryColumnExpression : SqlColumnExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.OuterApplyQueryColumn;

        // will not visit in this property because it's a reference
        public SqlQueryExpression OuterApplyQuery { get; }

        public SqlOuterApplyQueryColumnExpression(SqlExpression columnExpression, string columnAlias, ModelPath modelPath, SqlQueryExpression outerApplyQuery) : base(columnExpression, columnAlias, modelPath)
        {
            this.OuterApplyQuery = outerApplyQuery;
        }
    }
}
