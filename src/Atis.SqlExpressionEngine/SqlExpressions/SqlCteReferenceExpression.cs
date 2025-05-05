using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlCteReferenceExpression : SqlQuerySourceExpression
    {
        public SqlCteReferenceExpression(Guid cteAlias)
        {
            this.CteAlias = cteAlias;
        }

        public override SqlExpressionType NodeType => SqlExpressionType.CteReference;
        public Guid CteAlias { get; }

        public override HashSet<ColumnModelPath> GetColumnModelMap()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return DebugAliasGenerator.GetAlias(this.CteAlias, "cte");
        }
    }
}
