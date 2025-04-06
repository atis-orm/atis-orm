using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql.SqlExpressions
{
    public class SqlKeywordExpression : SqlExpression
    {
        public string Keyword { get; }

        public SqlKeywordExpression(string keyword)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
        }

        public override SqlExpressionType NodeType => SqlExpressionType.Keyword;

        public override string ToString()
        {
            return Keyword;
        }
    }
}
