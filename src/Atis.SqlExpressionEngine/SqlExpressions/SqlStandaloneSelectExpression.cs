using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        /// <param name="projection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SqlStandaloneSelectExpression(SqlExpression projection)
        {
            this.QueryShape = projection ?? throw new ArgumentNullException(nameof(projection));
            this.SelectList = ExtensionMethods.ConvertQueryShapeToSelectList(this.QueryShape, applyAll: true);
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.StandaloneSelect;

        /// <summary>
        /// 
        /// </summary>
        public SqlExpression QueryShape { get; }
        public IReadOnlyList<SelectColumn> SelectList { get; }

        public override SqlDataSourceQueryShapeExpression CreateQueryShape(Guid dataSourceAlias)
        {
            if (this.SelectList.Count == 1 &&
                this.SelectList[0].ScalarColumn)
                return new SqlDataSourceQueryShapeExpression(new SqlDataSourceColumnExpression(dataSourceAlias, this.SelectList[0].Alias), dataSourceAlias);

            var memberInit = this.QueryShape as SqlMemberInitExpression
                                ??
                                throw new InvalidOperationException($"QueryShape type '{this.QueryShape.GetType().Name}' is not supported.");
            var result = this.UpdateQueryShapeWithNewAlias(memberInit, dataSourceAlias, this.SelectList);
            return new SqlDataSourceQueryShapeExpression(result, dataSourceAlias);
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
        public SqlExpression Update(SqlExpression projection)
        {
            if (this.QueryShape == projection)
                return this;

            return new SqlStandaloneSelectExpression(projection);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var columns = this.ConvertToString(this.QueryShape, memberName: null);
            return $"(select {columns})";
        }

        private string ConvertToString(SqlExpression sqlExpression, string memberName)
        {
            if (sqlExpression is SqlMemberInitExpression queryShape)
            {
                var columns = new StringBuilder();
                foreach (var binding in queryShape.Bindings)
                {
                    if (columns.Length > 0)
                        columns.Append(", ");
                    columns.Append(this.ConvertToString(binding.SqlExpression, binding.MemberName));
                }
                return columns.ToString();
            }
            else
            {
                return $"{sqlExpression} as {memberName ?? "Col1"}";
            }
        }
    }
}
