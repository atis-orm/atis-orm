using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlInsertIntoExpression : SqlExpression
    {
        public SqlInsertIntoExpression(SqlTable sqlTable, IReadOnlyList<TableColumn> tableColumns, SqlDerivedTableExpression selectQuery)
        {
            this.SqlTable = sqlTable ?? throw new ArgumentNullException(nameof(sqlTable));
            this.TableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
            this.SelectQuery = selectQuery ?? throw new ArgumentNullException(nameof(selectQuery));

            if (this.TableColumns.IsNullOrEmpty())
            {
                throw new ArgumentException("Table columns cannot be empty.", nameof(tableColumns));
            }
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.InsertInto;

        public SqlTable SqlTable { get; }
        public IReadOnlyList<TableColumn> TableColumns { get; }
        public SqlDerivedTableExpression SelectQuery { get; }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlInsertInto(this);
        }

        public SqlInsertIntoExpression Update(SqlDerivedTableExpression selectQuery)
        {
            if (selectQuery == this.SelectQuery)
                return this;
            return new SqlInsertIntoExpression(this.SqlTable, this.TableColumns, selectQuery);
        }

        public override string ToString()
        {
            return $"INSERT INTO: {this.SelectQuery}";
        }
    }
}
