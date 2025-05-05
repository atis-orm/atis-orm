using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlSelectListExpression : SqlExpression
    {
        public SqlSelectListExpression(SelectColumn[] selectColumns)
        {
            if (!(selectColumns?.Length > 0))
                throw new ArgumentNullException(nameof(selectColumns));
            if (selectColumns.GroupBy(x => x.ModelPath).Any(y => y.Count() > 1))
                throw new ArgumentException("Select Columns must have unique model paths.", nameof(selectColumns));
            if (selectColumns.Any(x => x.ColumnExpression is SqlSelectListExpression))
                throw new ArgumentException("Select Columns cannot contain select list expressions.", nameof(selectColumns));
            this.SelectColumns = selectColumns;
        }
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.SelectColumnCollection;
        public SelectColumn[] SelectColumns { get; }


        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlSelectList(this);
        }

        public SqlSelectListExpression Update(SelectColumn[] selectColumns)
        {
            if (this.SelectColumns.AllEqual(selectColumns))
                return this;
            return new SqlSelectListExpression(selectColumns);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join(", ", this.SelectColumns.Select(x => x.ToString()));
        }
    }
}
