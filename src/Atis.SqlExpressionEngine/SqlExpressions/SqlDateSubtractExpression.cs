using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlDateSubtractExpression : SqlExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.DateSubtract;
        public SqlExpression StartDate { get; }
        public SqlExpression EndDate { get; }
        public SqlDatePart DatePart { get; }

        public SqlDateSubtractExpression(SqlDatePart datePart, SqlExpression startDate, SqlExpression endDate)
        {
            this.DatePart = datePart;
            this.StartDate = startDate ?? throw new ArgumentNullException(nameof(startDate));            
            this.EndDate = endDate ?? throw new ArgumentNullException(nameof(endDate));
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlDateSubtract(this);
        }

        public SqlDateSubtractExpression Update(SqlExpression startDate, SqlExpression endDate)
        {
            if (startDate == this.StartDate && endDate == this.EndDate)
            {
                return this;
            }
            return new SqlDateSubtractExpression(this.DatePart, startDate, endDate);
        }

        public override string ToString()
        {
            return $"dateSubtract({this.DatePart}, {this.StartDate}, {this.EndDate})";
        }
    }
}
