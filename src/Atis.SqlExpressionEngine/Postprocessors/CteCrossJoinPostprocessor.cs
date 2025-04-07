using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.Postprocessors
{
    public class CteCrossJoinPostprocessor : SqlExpressionVisitor, ISqlExpressionPostprocessor
    {
        private class CteUsage
        {
            public SqlDataSourceExpression CteConsumer { get; set; }
            public SqlDataSourceExpression NewCteReferenceToAdd { get; set; }
        }

        private readonly Stack<SqlDataSourceExpression> dataSourceStack = new Stack<SqlDataSourceExpression>();
        private readonly List<CteUsage> cteUsages = new List<CteUsage>();
        private readonly ISqlExpressionFactory sqlFactory;

        public CteCrossJoinPostprocessor(ISqlExpressionFactory sqlFactory)
        {
            this.sqlFactory = sqlFactory;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // CRITICAL: to clear these
            this.dataSourceStack.Clear();
            this.cteUsages.Clear();
        }

        public SqlExpression Postprocess(SqlExpression sqlExpression)
        {
            sqlExpression = this.Visit(sqlExpression);
            return sqlExpression;
        }

        protected internal override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
        {
            if (sqlDataSourceColumnExpression.DataSource.NodeType == SqlExpressionType.CteDataSource)
            {
                var consumerDataSource = this.dataSourceStack.Count > 0 ? this.dataSourceStack.Peek() : null;
                if (consumerDataSource != null)
                {
                    if (consumerDataSource.NodeType == SqlExpressionType.CteDataSource)
                    {
                        var cteReference = this.sqlFactory.CreateCteReference(sqlDataSourceColumnExpression.DataSource.DataSourceAlias);
                        var cteReferenceDataSource =this.sqlFactory.CreateDataSourceForCteReference(cteReference.CteAlias, cteReference);
                        // now this new data source (cteReferenceDataSource) needs to be added in the consumer CTE (lastDataSource.ParentSqlQuery)
                        // but we cannot modify the SqlQueryExpression here it will disturb the whole visit process
                        // when it reaches back to SqlQueryExpression Visit method
                        if (!cteUsages.Any(x => x.CteConsumer == consumerDataSource && x.NewCteReferenceToAdd == cteReferenceDataSource))
                        {
                            this.cteUsages.Add(new CteUsage
                            {
                                CteConsumer = consumerDataSource,
                                NewCteReferenceToAdd = cteReferenceDataSource
                            });
                        }
                        var newSqlDsColumn = this.sqlFactory.CreateDataSourceColumn(cteReferenceDataSource, sqlDataSourceColumnExpression.ColumnName);
                        // here we are changing the cte_1 (CTE data source reference).Column to cte_1 (CTE reference).Column
                        return newSqlDsColumn;
                    }
                }

            }
            return base.VisitSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
        }

        protected internal override SqlExpression VisitSqlQueryExpression(SqlQueryExpression node)
        {
            var updatedNode = base.VisitSqlQueryExpression(node);

            if (updatedNode is SqlQueryExpression sqlQuery)
            {
                var dataSourcesToAdd = this.cteUsages.Where(x => x.CteConsumer.QuerySource == node).ToArray();
                for (var i = 0; i < dataSourcesToAdd.Length; i++)
                {
                    var cteUsage = dataSourcesToAdd[i];
                    if (!(sqlQuery.AllQuerySources.Any(x => x.DataSourceAlias == cteUsage.NewCteReferenceToAdd.DataSourceAlias)))
                    {
                        sqlQuery.AddDataSource(cteUsage.NewCteReferenceToAdd);
                    }
                }
            }

            return updatedNode;
        }

        protected internal override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
        {
            this.dataSourceStack.Push(sqlDataSourceExpression);
            var updatedNode = base.VisitSqlDataSourceExpression(sqlDataSourceExpression);
            this.dataSourceStack.Pop();
            return updatedNode;
        }
    }
}
