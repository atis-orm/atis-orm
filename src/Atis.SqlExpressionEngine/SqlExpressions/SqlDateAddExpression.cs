using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    
    public class SqlDateAddExpression : SqlExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.DateAdd;
        public SqlExpression DateExpression { get; }
        public SqlDatePart DatePart { get; }
        public SqlExpression Interval { get; }

        public SqlDateAddExpression(SqlDatePart datePart, SqlExpression interval, SqlExpression dateExpression)
        {
            this.DateExpression = dateExpression ?? throw new ArgumentNullException(nameof(dateExpression));
            this.DatePart = datePart;
            this.Interval = interval ?? throw new ArgumentNullException(nameof(interval));
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlDateAdd(this);
        }

        public SqlDateAddExpression Update(SqlExpression interval, SqlExpression expression)
        {
            if (interval == this.Interval && expression == this.DateExpression)
            {
                return this;
            }
            return new SqlDateAddExpression(this.DatePart, interval, expression);
        }

        public override string ToString()
        {
            return $"dateAdd({this.DatePart}, {this.Interval}, {this.DateExpression})";
        }
    }
}
