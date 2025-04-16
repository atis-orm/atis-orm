using System;
using System.Collections.Generic;
using System.Linq;


namespace Atis.SqlExpressionEngine.SqlExpressions
{

public partial class SqlQueryExpression
    {
        class OuterDataSourceUsageInCteValidator : SqlExpressionVisitor
        {
            private readonly SqlQueryExpression sourceQuery;
            private readonly Stack<SqlDataSourceExpression> cteDataSource = new Stack<SqlDataSourceExpression>();

            public static void Validate(SqlQueryExpression sourceQuery, SqlExpression sqlExpression)
            {
                var visitor = new OuterDataSourceUsageInCteValidator(sourceQuery);
                visitor.Visit(sqlExpression);
            }

            public OuterDataSourceUsageInCteValidator(SqlQueryExpression sourceQuery)
            {
                this.sourceQuery = sourceQuery;
            }

            protected internal override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
            {
                var doPop = false;
                if (sqlDataSourceExpression.NodeType == SqlExpressionType.CteDataSource)
                {
                    doPop = true;
                    this.cteDataSource.Push(sqlDataSourceExpression);
                }
                this.ValidateOuterDataSourceUsageInCte(sqlDataSourceExpression.DataSourceAlias);
                var updatedNode = base.VisitSqlDataSourceExpression(sqlDataSourceExpression);
                if (doPop)
                    this.cteDataSource.Pop();
                return updatedNode;
            }

            protected internal override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
            {
                this.ValidateOuterDataSourceUsageInCte(sqlDataSourceColumnExpression.DataSourceReference.Reference.DataSourceAlias);
                return base.VisitSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
            }

            private void ValidateOuterDataSourceUsageInCte(Guid dataSourceAlias)
            {
                if (this.cteDataSource.Count > 0)
                    if (this.sourceQuery.AllQuerySources.Any(x => x.DataSourceAlias == dataSourceAlias))
                        throw new InvalidOperationException($"Outer data source is being used in a CTE Query.");
            }
        }
    }
}
