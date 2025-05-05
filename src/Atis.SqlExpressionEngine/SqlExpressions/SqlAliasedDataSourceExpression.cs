using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public abstract class SqlAliasedDataSourceExpression : SqlExpression
    {
        public abstract SqlQuerySourceExpression QuerySource { get; }
        public abstract Guid Alias { get; }
    }
}
