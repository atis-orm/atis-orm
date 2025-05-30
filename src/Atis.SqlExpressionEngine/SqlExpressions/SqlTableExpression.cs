using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// Represents a table in the SQL query.
    /// </summary>
    public class SqlTableExpression : SqlQuerySourceExpression
    {
        private readonly Dictionary<string, string> propertyMap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlTable"></param>
        /// <param name="tableColumns"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SqlTableExpression(SqlTable sqlTable, IReadOnlyList<TableColumn> tableColumns)
        {
            this.SqlTable = sqlTable ?? throw new ArgumentNullException(nameof(sqlTable));
            this.TableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
            this.propertyMap = tableColumns.ToDictionary(x => x.ModelPropertyName, x => x.DatabaseColumnName);
        }

        /// <summary>
        /// 
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Table;

        /// <summary>
        /// 
        /// </summary>
        public SqlTable SqlTable { get; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<TableColumn> TableColumns { get; }

        /// <inheritdoc />
        public override SqlDataSourceQueryShapeExpression CreateQueryShape(Guid dataSourceAlias)
        {
            var bindings = this.TableColumns.Select(x => new SqlMemberAssignment(x.ModelPropertyName, new SqlDataSourceColumnExpression(dataSourceAlias, x.DatabaseColumnName)))
                                                .ToArray();
            var memberInit = new SqlMemberInitExpression(bindings);
            return new SqlDataSourceQueryShapeExpression(memberInit, dataSourceAlias);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GetByPropertyName(string propertyName)
        {
            if (this.propertyMap.TryGetValue(propertyName, out var columnName))
                return columnName;
            throw new InvalidOperationException($"Property '{propertyName}' not found in table '{this.SqlTable}'.");
        }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlTable(this);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.SqlTable.TableName;
        }
    }
}
