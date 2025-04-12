using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlKeywordExpression : SqlExpression
    {
        public string Keyword { get; }

        public SqlKeywordExpression(string keyword)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
        }

        public override SqlExpressionType NodeType => SqlExpressionType.Keyword;

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitKeywordExpression(this);
        }

        public override string ToString()
        {
            return Keyword;
        }
    }
}
