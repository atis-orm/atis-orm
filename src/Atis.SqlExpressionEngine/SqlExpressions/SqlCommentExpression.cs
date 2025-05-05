using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlCommentExpression : SqlExpression
    {
        public SqlCommentExpression(string comment)
        {
            if (comment is null || string.IsNullOrWhiteSpace(comment))
                throw new ArgumentNullException(nameof(comment));
            if (comment.Any(x => !char.IsLetterOrDigit(x) && x != ' '))
                throw new ArgumentException($"Comment '{comment}' contains invalid characters.", nameof(comment));
            if (comment.Length > 500)
                throw new ArgumentException($"Comment '{comment}' exceeds the maximum length of 500 characters.", nameof(comment));
            this.Comment = comment;
        }

        public override SqlExpressionType NodeType => SqlExpressionType.Comment;
        public string Comment { get; }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlComment(this);
        }

        public override string ToString()
        {
            return $"/*{Comment}*/";
        }
    }
}
