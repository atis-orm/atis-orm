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

        public override SqlDataSourceQueryShapeExpression CreateQueryShape(Guid dataSourceAlias)
        {
            throw new NotImplementedException();
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlCteReference(this);
        }

        public override string ToString()
        {
            return DebugAliasGenerator.GetAlias(this.CteAlias, "cte");
        }
    }
}
