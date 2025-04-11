using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlDatePartExpression : SqlExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.DatePart;

        public SqlExpression DateExpression { get; }
        public SqlDatePart DatePart { get; }

        public SqlDatePartExpression(SqlDatePart datePart, SqlExpression dateExpression)
        {
            this.DateExpression = dateExpression ?? throw new ArgumentNullException(nameof(dateExpression));
            this.DatePart = datePart;
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlDatePartExpression(this);
        }

        public SqlDatePartExpression Update(SqlExpression dateExpression)
        {
            if (dateExpression == this.DateExpression)
            {
                return this;
            }
            return new SqlDatePartExpression(this.DatePart, dateExpression);
        }

        public override string ToString()
        {
            return $"datePart({this.DatePart}, {this.DateExpression})";
        }
    }
}
