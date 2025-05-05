using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlDefaultIfEmptyExpression : SqlExpression
    {
        public SqlDefaultIfEmptyExpression(SqlDerivedTableExpression derivedTable)
        {
            this.DerivedTable = derivedTable;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.DefaultIfEmpty;
        public SqlDerivedTableExpression DerivedTable { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlDefaultIfEmpty(this);
        }

        public SqlDefaultIfEmptyExpression Update(SqlDerivedTableExpression derivedTable)
        {
            if (derivedTable == DerivedTable)
            {
                return this;
            }
            return new SqlDefaultIfEmptyExpression(derivedTable);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"defaultIfEmpty({DerivedTable})";
        }
    }
}
