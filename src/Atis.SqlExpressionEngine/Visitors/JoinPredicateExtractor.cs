using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.Visitors
{
    public class JoinPredicateExtractor : SqlExpressionVisitor
    {
        private readonly HashSet<Guid> externalDataSourceAliases = new HashSet<Guid>();
        // we cannot just use externalDataSourceAliases because it will only contain
        // the data sources of the source query that we are focusing on, but this is
        // possible that the target derived table that we are trying to join is using
        // some other data sources which are not part of our source query, in that
        // case we still cannot join the derived table with the source query
        private readonly HashSet<Guid> internalDataSourceAliases = new HashSet<Guid>();
        private readonly List<SqlBinaryExpression> predicateExpressions = new List<SqlBinaryExpression>();
        private readonly Stack<SqlExpression> stack = new Stack<SqlExpression>();
        private readonly Stack<bool> targetWhereClauseStack = new Stack<bool>();
        private readonly SqlDerivedTableExpression derivedTableToJoin;
        private readonly List<SqlExpression> otherTablePredicateSides = new List<SqlExpression>();

        public bool JoinIsNotPossible { get; private set; }
        public IReadOnlyCollection<SqlBinaryExpression> PredicateExpressions => this.predicateExpressions;

        public JoinPredicateExtractor(SqlDerivedTableExpression derivedTableToJoin, HashSet<Guid> externalDataSourceAliases)
        {
            this.derivedTableToJoin = derivedTableToJoin;
            this.externalDataSourceAliases = externalDataSourceAliases;
        }

        public static bool TryExtractJoinPredicates(SqlDerivedTableExpression deriveTableToJoin, HashSet<Guid> externalDataSourceAliases, out SqlDerivedTableExpression derivedTableToJoinNormalized, out IReadOnlyCollection<SqlBinaryExpression> joinPredicates, out IReadOnlyCollection<SqlExpression> otherTablePredicateSides)
        {
            var extractor = new JoinPredicateExtractor(deriveTableToJoin, externalDataSourceAliases);
            var visitedExpression = extractor.Visit(deriveTableToJoin);
            var visitedDerivedTable = visitedExpression as SqlDerivedTableExpression
                                        ??
                                        throw new InvalidOperationException($"{nameof(JoinPredicateExtractor)} tried to extract the predicates for this it visited the given {nameof(derivedTableToJoin)} parameter but after visit the expression became {visitedExpression.GetType().Name} instead of {nameof(SqlDerivedTableExpression)}");
            if (extractor.JoinIsNotPossible)
            {
                derivedTableToJoinNormalized = null;
                joinPredicates = null;
                otherTablePredicateSides = null;
                return false;
            }
            else
            {
                var removeRedundantTrue = new RemoveRedundantTrueVisitor();
                visitedDerivedTable = removeRedundantTrue.Visit(visitedDerivedTable) as SqlDerivedTableExpression
                                        ??
                                        throw new InvalidOperationException($"Expected {nameof(SqlDerivedTableExpression)} but got {visitedDerivedTable.GetType().Name}");

                var newSelectColumnsToAdd = GetProjectionsToBeAdded(visitedDerivedTable, extractor.PredicateExpressions);
                if (newSelectColumnsToAdd.Count > 0)
                {
                    var fullSelectColumnList = visitedDerivedTable.SelectColumnCollection.SelectColumns.Concat(newSelectColumnsToAdd).ToArray();
                    var selectListExpression = new SqlSelectListExpression(fullSelectColumnList);
                    visitedDerivedTable = visitedDerivedTable.Update(visitedDerivedTable.CteDataSources, visitedDerivedTable.FromSource, visitedDerivedTable.Joins, visitedDerivedTable.WhereClause, visitedDerivedTable.GroupByClause, visitedDerivedTable.HavingClause, visitedDerivedTable.OrderByClause, selectListExpression);
                }

                derivedTableToJoinNormalized = visitedDerivedTable;
                joinPredicates = extractor.PredicateExpressions;
                otherTablePredicateSides = extractor.otherTablePredicateSides;
                return true;
            }
        }

        private static IReadOnlyCollection<SelectColumn> GetProjectionsToBeAdded(SqlDerivedTableExpression derivedTable, IEnumerable<SqlExpression> sqlExpressionsToSearch)
        {
            var myDataSourceAliases = new HashSet<Guid>(derivedTable.AllDataSources.Select(x => x.Alias));

            var alreadyProjectedColumns = new HashSet<SqlDataSourceColumnExpression>(
                derivedTable.SelectColumnCollection.SelectColumns
                    .Select(x => x.ColumnExpression)
                    .OfType<SqlDataSourceColumnExpression>()
            );

            var newEntries = new HashSet<SqlDataSourceColumnExpression>();
            foreach (var sqlExpression in sqlExpressionsToSearch)
            {
                var usedColumns = DataSourceColumnUsageExtractor
                    .FindDataSources(myDataSourceAliases)
                    .In(sqlExpression)
                    .ExtractDataSourceColumnExpressions();

                foreach (var column in usedColumns)
                {
                    if (!alreadyProjectedColumns.Contains(column))
                        newEntries.Add(column);
                }
            }

            var updatedExpressionList = new List<SelectColumn>();
            int colIndex = 1;
            foreach (var col in newEntries)
            {
                string alias = $"SubQueryCol{colIndex++}";
                var binding = new SqlMemberAssignment(alias, col);
                var selectItem = new SelectColumn(binding.SqlExpression, alias, scalarColumn: false);
                updatedExpressionList.Add(selectItem);
            }

            return updatedExpressionList;
        }

        private bool IsVisitingTargetWhereClause()
        {
            if (this.targetWhereClauseStack.Count == 0)
                return false;
            return this.targetWhereClauseStack.Peek();
        }

        private bool IsWhereOrNode(SqlExpression node)
        {
            var filterCondition = this.derivedTableToJoin.WhereClause?.FilterConditions.Where(x => x.Predicate == node).FirstOrDefault();
            return filterCondition?.UseOrOperator == true;
        }

        private bool IsExternalDataSourceAlias(Guid dataSourceAlias)
        {
            return !this.internalDataSourceAliases.Contains(dataSourceAlias);
        }

        private bool HasSourceDataSourceReference(SqlExpression node)
        {
            return ExternalDataSourceReferenceFinder.Find(this.externalDataSourceAliases).In(node);
        }

        private bool CheckOrElseComparison()
        {
            if (!this.JoinIsNotPossible)
            {
                var stackToArray = this.stack.ToArray();
                for (var i = 0; i < stackToArray.Length; i++)
                {
                    if (stackToArray[i] is SqlBinaryExpression binaryExpression)
                    {
                        if (binaryExpression.NodeType == SqlExpressionType.OrElse)
                        {
                            this.JoinIsNotPossible = true;
                            break;
                        }
                    }
                }
            }
            return this.JoinIsNotPossible;
        }

        public override SqlExpression Visit(SqlExpression node)
        {
            if (this.JoinIsNotPossible)
                return node;

            if (node is null) return null;

            if (this.IsWhereOrNode(node))
            {
                if (this.HasSourceDataSourceReference(node))
                {
                    this.JoinIsNotPossible = true;
                    return node;
                }
            }

            this.stack.Push(node);
            var visited = base.Visit(node);
            this.stack.Pop();
            return visited;
        }

        protected internal override SqlExpression VisitSqlBinary(SqlBinaryExpression node)
        {
            // you might think that we are simply adding the predicate without looking whether
            // the binary expression is being in the correct position or not, but this does
            // NOT matter because the VisitSqlDataSourceColumn method is checking if the outer
            // column is used anywhere outside the top-level Where clause then it will mark the
            // JoinIsNotPossible, therefore, these will not count.
            var result = base.VisitSqlBinary(node);
            switch (result.NodeType)
            {
                case SqlExpressionType.Equal:
                case SqlExpressionType.NotEqual:
                case SqlExpressionType.GreaterThanOrEqual:
                case SqlExpressionType.GreaterThan:
                case SqlExpressionType.LessThanOrEqual:
                case SqlExpressionType.LessThan:
                    var hasSourceParameterInLeft = this.HasSourceDataSourceReference(node.Left);
                    var hasSourceParameterInRight = this.HasSourceDataSourceReference(node.Right);
                    if (!hasSourceParameterInLeft)
                    {
                        this.otherTablePredicateSides.Add(node.Left);
                    }
                    else if (!hasSourceParameterInRight)
                    {
                        this.otherTablePredicateSides.Add(node.Right);
                    }
                    if (hasSourceParameterInLeft || hasSourceParameterInRight)
                    {
                        if (!this.CheckOrElseComparison())
                        {
                            this.predicateExpressions.Add(node);
                            return new SqlLiteralExpression(true);
                        }
                    }
                    break;
            }
            return result;
        }

        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            if (!this.IsVisitingTargetWhereClause())                        // if not visiting where clause of sourceQuery
            {
                if (this.IsExternalDataSourceAlias(node.DataSourceAlias))   // if the data source alias is external
                {
                    this.JoinIsNotPossible = true;                          // it means external data source is being used
                    return node;                                            // in some other parts of query so join is not possible
                }
            }
            return base.VisitSqlDataSourceColumn(node);
        }

        protected internal override SqlExpression VisitFilterClause(SqlFilterClauseExpression node)
        {
            this.targetWhereClauseStack.Push(node == this.derivedTableToJoin.WhereClause);
            var visited = base.VisitFilterClause(node);
            this.targetWhereClauseStack.Pop();
            return visited;
        }

        protected internal override SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
        {
            foreach (var aliasedDataSource in node.AllDataSources)
            {
                internalDataSourceAliases.Add(aliasedDataSource.Alias);
            }
            var visited = base.VisitSqlDerivedTable(node);
            foreach (var aliasedDataSource in node.AllDataSources)
            {
                internalDataSourceAliases.Remove(aliasedDataSource.Alias);
            }
            return visited;
        }
    }

    class ExternalDataSourceReferenceFinder : SqlExpressionVisitor
    {
        private readonly HashSet<Guid> externalDataSourceAliases;
        public bool ExternalDataSourceReferenceFound { get; private set; }

        public ExternalDataSourceReferenceFinder(HashSet<Guid> externalDataSourceAliases)
        {
            this.externalDataSourceAliases = externalDataSourceAliases ?? throw new ArgumentNullException(nameof(externalDataSourceAliases));
        }

        public static bool FindIn(SqlExpression node, HashSet<Guid> dataSourceAlias)
        {
            var finder = new ExternalDataSourceReferenceFinder(dataSourceAlias);
            finder.Visit(node);
            return finder.ExternalDataSourceReferenceFound;
        }

        public static ExternalDataSourceReferenceFinder Find(HashSet<Guid> externalDataSourceAliases)
        {
            return new ExternalDataSourceReferenceFinder(externalDataSourceAliases);
        }

        public bool In(SqlExpression node)
        {
            this.ExternalDataSourceReferenceFound = false;
            this.Visit(node);
            return this.ExternalDataSourceReferenceFound;
        }

        public override SqlExpression Visit(SqlExpression node)
        {
            if (this.ExternalDataSourceReferenceFound)
                return node;
            return base.Visit(node);
        }

        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            // if incoming data source alias is found in external data source aliases
            if (this.externalDataSourceAliases.Contains(node.DataSourceAlias))
            {
                this.ExternalDataSourceReferenceFound = true;
                return node;
            }
            return base.VisitSqlDataSourceColumn(node);
        }
    }

    class RemoveRedundantTrueVisitor : SqlExpressionVisitor
    {
        /// <inheritdoc />
        protected internal override SqlExpression VisitSqlBinary(SqlBinaryExpression node)
        {
            // Remove 'true' conditions from logical AND operations
            if (node.NodeType == SqlExpressionType.AndAlso)
            {
                if (node.Left is SqlLiteralExpression leftConstant && leftConstant.LiteralValue is bool b1 && b1)
                {
                    return Visit(node.Right);
                }
                if (node.Right is SqlLiteralExpression rightConstant && rightConstant.LiteralValue is bool b2 && b2)
                {
                    return Visit(node.Left);
                }
            }
            return base.VisitSqlBinary(node);
        }

        protected internal override SqlExpression VisitFilterClause(SqlFilterClauseExpression node)
        {
            var visited = base.VisitFilterClause(node) as SqlFilterClauseExpression
                            ??
                            throw new InvalidOperationException($"Expected {nameof(SqlFilterClauseExpression)} but got {node.GetType().Name}");

            if (visited.NodeType != SqlExpressionType.WhereClause)
                return visited;

            var newFilterConditions = new List<FilterCondition>();
            foreach (var filter in visited.FilterConditions)
            {
                if (filter.Predicate is SqlLiteralExpression literal && literal.LiteralValue is bool b && b)
                {
                    // Skip the 'true' condition
                    continue;
                }
                newFilterConditions.Add(filter);
            }
            if (newFilterConditions.Count != visited.FilterConditions.Length)
            {
                if (newFilterConditions.Count == 0)
                {
                    return null;
                }
                else
                {
                    return new SqlFilterClauseExpression(newFilterConditions.ToArray(), visited.NodeType);
                }
            }
            return visited;
        }
    }
}
