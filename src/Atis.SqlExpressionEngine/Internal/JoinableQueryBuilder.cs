using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.Internal
{
    public class JoinableQueryBuilderResult
    {
        public JoinableQueryBuilderResult(SqlDerivedTableExpression normalizedDerivedTable, SqlExpression joinCondition)
        {
            this.NormalizedDerivedTable = normalizedDerivedTable;
            this.JoinCondition = joinCondition;
        }

        public SqlDerivedTableExpression NormalizedDerivedTable { get;  }
        public SqlExpression JoinCondition { get; }
    }

    public partial class JoinableQueryBuilder
    {
        private readonly SqlDerivedTableExpression derivedTable;
        private readonly HashSet<Guid> externalDataSourceAliases;
        private readonly ISqlExpressionFactory sqlFactory;
        private readonly Guid newDataSourceAlias;

        public JoinableQueryBuilder(SqlDerivedTableExpression derivedTable, HashSet<Guid> externalDataSourceAliases, ISqlExpressionFactory sqlFactory, Guid newDataSourceAlias)
        {
            /* derivedTable will be something like this
             * 
             * select   a_1.Field1, a_1.Field2
             * from     Table1 as a_1
             * where    a_1.Field2 > 5
             *          and outerTable.Field1 = a_1.Field1
             * 
             * given derived table will be a query having multiple tables or having 
             * 
             * 1. The derived table must have only 1 data source, no mater what that data source is
             * 2. The derived table must have a join condition which is using the outer parameter
             * 3. The outer parameter must not be used anywhere else in within derived table except for where clause
             * 4. The outer parameter must not be used in OrElse comparison
             * 5. The derived table must not have any other thing set except for where clause and auto projection
             * 
             * Once above conditions are met, we will get the first data source this first data source will
             * be added as a data source in the current SqlSelectExpression, when doing this SqlSelectExpression
             * will give a new Data Source Alias to it which will not match with the data source alias in the
             * actual derived table so we will be needing to visit the actual derived table and replace all
             * the data source aliases with the new one. The only place we'll be needing to do that is
             * the where clause.
            */
            this.derivedTable = derivedTable ?? throw new ArgumentNullException(nameof(derivedTable));
            this.externalDataSourceAliases = externalDataSourceAliases ?? throw new ArgumentNullException(nameof(externalDataSourceAliases));
            this.sqlFactory = sqlFactory;
            this.newDataSourceAlias = newDataSourceAlias;
        }

        protected virtual bool IsJoinable()
        {
            return this.derivedTable.AutoProjection &&
                    this.derivedTable.AllDataSources.Count == 1 &&
                    this.derivedTable.Joins.Length == 0 &&
                    this.derivedTable.CteDataSources.Length == 0 &&
                    !(this.derivedTable.HavingClause?.FilterConditions.Length > 0) &&
                    this.derivedTable.GroupByClause.Length == 0 &&
                    !(this.derivedTable.OrderByClause?.OrderByColumns.Length > 0) &&
                    this.derivedTable.Top == null &&
                    this.derivedTable.IsDistinct == false &&
                    this.derivedTable.RowOffset == null &&
                    this.derivedTable.RowsPerPage == null &&
                    this.derivedTable.WhereClause?.FilterConditions.Length > 0;
        }

        public bool TryBuild(out JoinableQueryBuilderResult result)
        {
            if (this.IsJoinable())
            {
                if (JoinPredicateExtractor.TryExtractJoinPredicates(derivedTable, this.externalDataSourceAliases, out var derivedTableToJoinNormalized, out var joinConditions))
                {
                    SqlExpression joinCondition;
                    if (joinConditions?.Count > 0)
                    {
                        joinCondition = this.CombinePredicates(joinConditions);
                        var dataSourceAliasReplacer = new ReplaceDataSourceAliasVisitor(derivedTable.FromSource.Alias, this.newDataSourceAlias);
                        joinCondition = dataSourceAliasReplacer.Visit(joinCondition);
                    }
                    else
                    {
                        joinCondition = null;
                    }

                    result = new JoinableQueryBuilderResult(derivedTableToJoinNormalized, joinCondition);
                    return true;
                }
            }
            result = null;
            return false;
        }
        private SqlExpression CombinePredicates(IEnumerable<SqlExpression> predicates)
        {
            var combinedPredicates = predicates.Aggregate((accumulatedExpression, nextPredicate) => this.sqlFactory.CreateBinary(accumulatedExpression, nextPredicate, SqlExpressionType.AndAlso));
            return combinedPredicates;
        }


        private class JoinPredicateExtractor : SqlExpressionVisitor
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

            public bool JoinIsNotPossible { get; private set; }
            public IReadOnlyCollection<SqlBinaryExpression> PredicateExpressions => this.predicateExpressions;

            public JoinPredicateExtractor(SqlDerivedTableExpression derivedTableToJoin, HashSet<Guid> externalDataSourceAliases)
            {
                this.derivedTableToJoin = derivedTableToJoin;
                this.externalDataSourceAliases = externalDataSourceAliases;
            }

            public static bool TryExtractJoinPredicates(SqlDerivedTableExpression deriveTableToJoin, HashSet<Guid> externalDataSourceAliases, out SqlDerivedTableExpression derivedTableToJoinNormalized, out IReadOnlyCollection<SqlBinaryExpression> joinPredicates)
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
                    return false;
                }
                else
                {
                    var removeRedundantTrue = new RemoveRedundantTrueVisitor();
                    visitedDerivedTable = removeRedundantTrue.Visit(visitedDerivedTable) as SqlDerivedTableExpression
                                            ??
                                            throw new InvalidOperationException($"Expected {nameof(SqlDerivedTableExpression)} but got {visitedDerivedTable.GetType().Name}");

                    derivedTableToJoinNormalized = visitedDerivedTable;
                    joinPredicates = extractor.PredicateExpressions;
                    return true;
                }
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
                if (node == this.derivedTableToJoin.WhereClause)
                {
                    this.targetWhereClauseStack.Push(true);
                }
                else
                {
                    this.targetWhereClauseStack.Push(false);
                }
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

        private class ExternalDataSourceReferenceFinder : SqlExpressionVisitor
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

        private class RemoveRedundantTrueVisitor : SqlExpressionVisitor
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
}
