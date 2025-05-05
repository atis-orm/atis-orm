using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlFilterClauseExpression : SqlExpression
    {
        private static readonly ISet<SqlExpressionType> _allowedTypes = new HashSet<SqlExpressionType>
            {
                SqlExpressionType.WhereClause,
                SqlExpressionType.HavingClause,
            };
        private static SqlExpressionType ValidateNodeType(SqlExpressionType nodeType)
            => _allowedTypes.Contains(nodeType)
                ? nodeType
                : throw new InvalidOperationException($"SqlExpressionType '{nodeType}' is not a valid Filter Clause.");

        public SqlFilterClauseExpression(FilterCondition[] filterConditions, SqlExpressionType nodeType)
        {
            if (!(filterConditions?.Length > 0))
                throw new ArgumentNullException(nameof(filterConditions), "Filter conditions cannot be null or empty.");
            this.FilterConditions = filterConditions;
            this.NodeType = ValidateNodeType(nodeType);
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType { get; }
        public FilterCondition[] FilterConditions { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitFilterClause(this);
        }

        public SqlFilterClauseExpression Update(FilterCondition[] filterConditions)
        {
            if (this.FilterConditions.AllEqual(filterConditions))
                return this;
            return new SqlFilterClauseExpression(filterConditions, this.NodeType);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var filters = string.Concat(this.FilterConditions.Select((x, i) => $"{(i > 0 ? (x.UseOrOperator ? "or " : "and ") : string.Empty)}{x.Predicate}"));
            return $"{this.NodeType} {filters}";
        }
    }
}
