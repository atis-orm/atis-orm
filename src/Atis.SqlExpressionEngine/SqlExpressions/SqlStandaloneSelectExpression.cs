using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlStandaloneSelectExpression : SqlSubQuerySourceExpression
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectList"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SqlStandaloneSelectExpression(SelectColumn[] selectList)
        {
            if (!(selectList?.Length > 0))
                throw new ArgumentNullException(nameof(selectList));

            this.SelectList = selectList;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.StandaloneSelect;

        /// <summary>
        /// 
        /// </summary>
        public SelectColumn[] SelectList { get; }

        /// <inheritdoc />
        public override HashSet<ColumnModelPath> GetColumnModelMap()
        {
            return new HashSet<ColumnModelPath>(this.SelectList.Select(x => new ColumnModelPath(x.Alias, x.ModelPath)));
        }


        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlStandaloneSelect(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectColumns"></param>
        /// <returns></returns>
        public SqlExpression Update(SelectColumn[] selectColumns)
        {
            if (this.SelectList.AllEqual(selectColumns))
                return this;

            return new SqlStandaloneSelectExpression(selectColumns);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var selectColumnToString = string.Join(", ", this.SelectList.Select(x => x.ToString()));
            return $"(select {selectColumnToString})";
        }
    }
}
