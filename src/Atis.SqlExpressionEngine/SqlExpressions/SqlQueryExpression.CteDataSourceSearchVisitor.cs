using System;
using System.Collections.Generic;
using System.Linq;


namespace Atis.SqlExpressionEngine.SqlExpressions
{

public partial class SqlQueryExpression
    {
        class CteDataSourceSearchVisitor : SqlExpressionVisitor
        {
            private readonly SqlQueryExpression sourceQuery;
            private readonly Stack<SqlDataSourceExpression> cteDataSource = new Stack<SqlDataSourceExpression>();
            public bool HasOuterDataSource { get; private set; }

            public static bool Find(SqlQueryExpression sourceQuery, SqlExpression sqlExpression)
            {
                var visitor = new CteDataSourceSearchVisitor(sourceQuery);
                visitor.Visit(sqlExpression);
                return visitor.HasOuterDataSource;
            }

            public CteDataSourceSearchVisitor(SqlQueryExpression sourceQuery)
            {
                this.sourceQuery = sourceQuery;
            }

            public override SqlExpression Visit(SqlExpression node)
            {
                if (this.HasOuterDataSource)
                    return node;
                return base.Visit(node);
            }

            protected internal override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
            {
                var doPop = false;
                if (sqlDataSourceExpression.NodeType == SqlExpressionType.CteDataSource)
                {
                    doPop = true;
                    this.cteDataSource.Push(sqlDataSourceExpression);
                }
                this.FindOuterDataSource(sqlDataSourceExpression.DataSourceAlias);
                if (this.HasOuterDataSource)
                    return sqlDataSourceExpression;
                var updatedNode = base.VisitSqlDataSourceExpression(sqlDataSourceExpression);
                if (doPop)
                    this.cteDataSource.Pop();
                return updatedNode;
            }

            protected internal override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
            {
                this.FindOuterDataSource(sqlDataSourceColumnExpression.DataSource.DataSourceAlias);
                if (this.HasOuterDataSource)
                    return sqlDataSourceColumnExpression;
                return base.VisitSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
            }

            private void FindOuterDataSource(Guid dataSourceAlias)
            {
                if (this.cteDataSource.Count > 0)
                    if (this.sourceQuery.AllQuerySources.Any(x => x.DataSourceAlias == dataSourceAlias))
                        this.HasOuterDataSource = true;
                        //throw new InvalidOperationException($"Outer data source is being used in a CTE Query.");
            }
        }
    }
}
