using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlAliasedCteSourceExpression : SqlExpression
    {
        public SqlAliasedCteSourceExpression(SqlSubQuerySourceExpression cteBody, Guid cteAlias)
        {
            this.CteBody = cteBody ?? throw new ArgumentNullException(nameof(cteBody));
            this.CteAlias = cteAlias;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.CteDataSource;
        public SqlSubQuerySourceExpression CteBody { get; }
        public Guid CteAlias { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlAliasedCteSource(this);
        }

        public SqlAliasedCteSourceExpression Update(SqlSubQuerySourceExpression querySource)
        {
            if (querySource == this.CteBody)
                return this;
            return new SqlAliasedCteSourceExpression(querySource, this.CteAlias);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.CteBody} as {DebugAliasGenerator.GetAlias(this.CteAlias, "cte")}";
        }
    }
}
