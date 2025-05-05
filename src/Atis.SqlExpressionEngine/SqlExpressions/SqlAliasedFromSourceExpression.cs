using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlAliasedFromSourceExpression : SqlAliasedDataSourceExpression
    {
        public SqlAliasedFromSourceExpression(SqlQuerySourceExpression querySource, Guid alias)
        {
            this.QuerySource = querySource ?? throw new ArgumentNullException(nameof(querySource));
            this.Alias = alias;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.FromDataSource;
        public override SqlQuerySourceExpression QuerySource { get; }
        public override Guid Alias { get; }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlAliasedFromSource(this);
        }

        public SqlAliasedFromSourceExpression Update(SqlQuerySourceExpression querySource)
        {
            if (querySource == this.QuerySource)
                return this;
            return new SqlAliasedFromSourceExpression(querySource, this.Alias);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.QuerySource} as {DebugAliasGenerator.GetAlias(this)}";
        }
    }
}
