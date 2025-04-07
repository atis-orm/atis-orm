using System;
using System.Collections.Generic;
using System.Linq;


namespace Atis.SqlExpressionEngine.SqlExpressions
{

public partial class SqlQueryExpression
    {
        class SubQueryColumnAccessReplacementVisitor : SqlExpressionVisitor
        {
            private readonly SqlColumnExpression[] wrappedQueryProjections;
            private readonly Dictionary<SqlExpression, int> expressionHash = new Dictionary<SqlExpression, int>();
            private readonly SqlDataSourceExpression newDataSource;
            private readonly Func<SqlDataSourceExpression, string, SqlDataSourceColumnExpression> sqlDataSourceColumnFactory;
            private readonly SqlExpressionHashGenerator sqlExpressionHashGenerator = new SqlExpressionHashGenerator();

            public SubQueryColumnAccessReplacementVisitor(SqlColumnExpression[] wrappedQueryProjections, SqlDataSourceExpression newDataSource, Func<SqlDataSourceExpression, string, SqlDataSourceColumnExpression> sqlDataSourceColumnFactory)
            {
                // newDataSource = the first data source of outer query that is now wrapper
                //   old query which is pushing inside
                //   new query = outer query wrapping old query
                //   newDataSource = new query.DataSources[0]   <- this is pointing to old query

                this.wrappedQueryProjections = wrappedQueryProjections;
                foreach (var item in wrappedQueryProjections)
                {
                    var e = item.ColumnExpression;
                    expressionHash[e] = sqlExpressionHashGenerator.GenerateHash(e);
                }
                this.newDataSource = newDataSource;
                this.sqlDataSourceColumnFactory = sqlDataSourceColumnFactory;
            }

            public override SqlExpression Visit(SqlExpression node)
            {
                var subQueryColumn = this.wrappedQueryProjections.Where(x => x.ColumnExpression == node ||
                                                                                this.expressionHash[x.ColumnExpression] == sqlExpressionHashGenerator.GenerateHash(node))
                                                                    .FirstOrDefault();
                if (subQueryColumn != null)
                {
                    // here we are creating the new column access expression that is using innerQueryDataSource
                    return this.sqlDataSourceColumnFactory(newDataSource, subQueryColumn.ColumnAlias);
                }
                return base.Visit(node);
            }
        }
    }
}
