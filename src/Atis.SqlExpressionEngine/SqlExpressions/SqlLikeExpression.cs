using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlLikeExpression : SqlExpression
    {
        private static readonly ISet<SqlExpressionType> _allowedTypes = new HashSet<SqlExpressionType>
            {
                SqlExpressionType.Like,
                SqlExpressionType.LikeStartsWith,
                SqlExpressionType.LikeEndsWith,
            };
        private static SqlExpressionType ValidateNodeType(SqlExpressionType nodeType)
            => _allowedTypes.Contains(nodeType)
                ? nodeType
                : throw new InvalidOperationException($"SqlExpressionType '{nodeType}' is not a valid SqlLikeExpression");

        public SqlExpression Expression { get; }
        public SqlExpression Pattern { get; }
        public override SqlExpressionType NodeType { get; }

        public SqlLikeExpression(SqlExpression stringExpression, SqlExpression pattern, SqlExpressionType nodeType)
        {
            this.Expression = stringExpression ?? throw new ArgumentNullException(nameof(stringExpression));
            this.Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            ValidateNodeType(nodeType);
            this.NodeType = nodeType;
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlLikeExpression(this);
        }

        public SqlLikeExpression Update(SqlExpression stringExpression, SqlExpression pattern)
        {
            if (stringExpression == this.Expression && pattern == this.Pattern)
            {
                return this;
            }
            return new SqlLikeExpression(stringExpression, pattern, this.NodeType);
        }

        public override string ToString()
        {
            return $"{this.Expression} {this.NodeType} {this.Pattern}";
        }
    }
}
