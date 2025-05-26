using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.Internal
{
    class ReplaceDataSourceAliasVisitor : SqlExpressionVisitor
    {
        private readonly Dictionary<Guid, Guid> aliasMap;

        public ReplaceDataSourceAliasVisitor(Dictionary<Guid, Guid> aliasMap)
        {
            this.aliasMap = aliasMap ?? throw new ArgumentNullException(nameof(aliasMap));
        }

        public static SqlExpression FindAndReplace(Dictionary<Guid, Guid> aliasMap, SqlExpression sqlExpressionToSearch)
        {
            var visitor = new ReplaceDataSourceAliasVisitor(aliasMap);
            return visitor.Visit(sqlExpressionToSearch);
        }

        protected internal override SqlExpression VisitDataSourceQueryShape(SqlDataSourceQueryShapeExpression node)
        {
            var visited = base.VisitDataSourceQueryShape(node);
            if (visited is SqlDataSourceQueryShapeExpression dsQueryShape)
            {
                if (this.aliasMap.TryGetValue(dsQueryShape.DataSourceAlias, out var newAlias))
                {
                    visited = new SqlDataSourceQueryShapeExpression(dsQueryShape.ShapeExpression, newAlias);
                }
            }
            return visited;
        }

        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            if (this.aliasMap.TryGetValue(node.DataSourceAlias, out var newAlias))
            {
                return new SqlDataSourceColumnExpression(newAlias, node.ColumnName);
            }
            return base.VisitSqlDataSourceColumn(node);
        }
    }
}
