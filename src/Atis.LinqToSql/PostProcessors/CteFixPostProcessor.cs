using Atis.LinqToSql.SqlExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql.Postprocessors
{
    public class CteFixPostprocessor : SqlExpressionVisitor, IPostprocessor
    {
        private readonly List<SqlDataSourceExpression> cteDataSources = new List<SqlDataSourceExpression>();
        private readonly Stack<SqlExpression> expressionStack = new Stack<SqlExpression>();
        private readonly ISqlExpressionFactory sqlFactory;

        public CteFixPostprocessor(ISqlExpressionFactory sqlFactory)
        {
            this.sqlFactory = sqlFactory;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // CRITICAL: to clear these
            this.cteDataSources.Clear();
            this.expressionStack.Clear();
        }

        /// <inheritdoc />
        public SqlExpression Process(SqlExpression sqlExpression)
        {
            return this.Visit(sqlExpression);
        }

        /// <inheritdoc />
        public override SqlExpression Visit(SqlExpression node)
        {
            this.expressionStack.Push(node);
            var updatedNode = base.Visit(node);
            this.expressionStack.Pop();
            return updatedNode;
        }

        /// <inheritdoc />
        protected internal override SqlExpression VisitSqlQueryExpression(SqlQueryExpression node)
        {
            var updatedNode = base.VisitSqlQueryExpression(node);

            if (this.expressionStack.Count > 1)     // not the top-most sql query
            {
                if (updatedNode is SqlQueryExpression updatedQuery && updatedQuery.IsCte)
                {
                    var cteDataSource = updatedQuery.CteDataSources.First();
                    this.cteDataSources.Add(cteDataSource);

                    var cteAlias = cteDataSource.DataSourceAlias;
                    var cteReference = this.sqlFactory.CreateCteReference(cteAlias);
                    var newDataSource = this.sqlFactory.CreateDataSourceForCteReference(cteAlias, cteReference);
                    var updatedSqlQuery = updatedQuery.Update(newDataSource, updatedQuery.Joins, updatedQuery.WhereClause, updatedQuery.GroupBy, updatedQuery.Projection, updatedQuery.OrderBy, updatedQuery.Top, updatedQuery.CteDataSources, updatedQuery.HavingClause, updatedQuery.Unions);
                    updatedSqlQuery.SetAsNonCte();
                    return updatedSqlQuery;
                }
            }
            else if (updatedNode is SqlQueryExpression updatedQuery)
            {
                // this is the top-most query
                foreach (var cteDataSource in this.cteDataSources)
                {
                    // here it will create a detached copy of cteDataSource (data source)
                    var newDataSource = this.sqlFactory.CreateDataSourceCopy(cteDataSource);
                    // and when we call this method this will attach that copy
                    // to the query
                    updatedQuery.AddCteDataSource(newDataSource);
                }
            }
            return updatedNode;
        }
    }
}
