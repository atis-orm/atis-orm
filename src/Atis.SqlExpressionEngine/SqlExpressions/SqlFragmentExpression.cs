using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlFragmentExpression : SqlExpression
    {
        public SqlFragmentExpression(string fragment)
        {
            this.Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        }

        public override SqlExpressionType NodeType => SqlExpressionType.Fragment;
        public string Fragment { get; }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlFragment(this);
        }

        public override string ToString()
        {
            return this.Fragment;
        }
    }
}
