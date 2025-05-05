using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.Internal
{


    /// <summary>
    /// Visitor class to check if any other data source reference has been used.
    /// </summary>
    public class ExternalDataSourceUsageVisitor : SqlExpressionVisitor
    {
        private SqlDerivedTableExpression currentDerivedTable;
        private readonly Stack<SqlDerivedTableExpression> selectQueryStack = new Stack<SqlDerivedTableExpression>();
        public bool OtherDataSourceHasBeenUsed { get; private set; }

        /// <summary>
        /// Determines whether an outside data source has been used in the specified SQL query.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to check.</param>
        /// <returns><c>true</c> if an outside data source has been used; otherwise, <c>false</c>.</returns>
        public static bool HasExternalDataSourceBeenUsed(SqlDerivedTableExpression sqlQuery)
        {
            var visitor = new ExternalDataSourceUsageVisitor();
            visitor.Visit(sqlQuery);
            return visitor.OtherDataSourceHasBeenUsed;
        }

        /// <summary>
        /// Checks if the specified alias is part of the current SQL query's data sources.
        /// </summary>
        /// <param name="alias">The alias to check.</param>
        private void CheckIfThisIsMyDataSource(Guid alias)
        {
            if (OtherDataSourceHasBeenUsed)
                return;

            if (this.currentDerivedTable is null)
                return;

            var isMyDataSource = this.currentDerivedTable.AllDataSources.Any(x => x.Alias == alias);
            if (!isMyDataSource)
                this.OtherDataSourceHasBeenUsed = true;
        }

        /// <inheritdoc />
        protected internal override SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
        {
            try
            {
                this.selectQueryStack.Push(node);
                this.currentDerivedTable = node;
                return base.VisitSqlDerivedTable(node);
            }
            finally
            {
                this.currentDerivedTable = this.selectQueryStack.Pop();
            }
        }

        /// <inheritdoc />
        public override SqlExpression Visit(SqlExpression node)
        {
            if (this.OtherDataSourceHasBeenUsed)
                return node;
            return base.Visit(node);
        }

        /// <inheritdoc />
        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            this.CheckIfThisIsMyDataSource(node.DataSourceAlias);
            return node;
        }
    }
}
