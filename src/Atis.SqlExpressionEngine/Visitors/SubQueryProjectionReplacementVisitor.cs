using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.Visitors
{
    public class SubQueryProjectionReplacementVisitor : SqlExpressionVisitor
    {
        private class ReferenceReplacementFlag
        {
            public bool IsReplaced { get; set; }
        }

        //private readonly SelectColumn[] subQueryProjections;
        private readonly AliasedDataSource subQueryDataSource;
        private readonly Guid subQueryDataSourceAlias;
        private readonly SqlExpressionHashGenerator hashGenerator;
        private readonly List<(int, SelectColumn)> subQueryProjectionHashMap;
        private readonly Stack<ReferenceReplacementFlag> referenceReplaced = new Stack<ReferenceReplacementFlag>();
        private readonly Stack<SqlExpression> sqlExpressionStack = new Stack<SqlExpression>();
        private readonly Stack<bool> visitingCteDataSource = new Stack<bool>();

        public static SqlExpression FindAndReplace(SelectColumn[] subQueryProjections, AliasedDataSource ds, SqlExpression toFindIn)
        {
            if (toFindIn is null)
                throw new ArgumentNullException(nameof(toFindIn));
            if (subQueryProjections is null)
                throw new ArgumentNullException(nameof(subQueryProjections));
            if (ds is null)
                throw new ArgumentNullException(nameof(ds));
            var visitor = new SubQueryProjectionReplacementVisitor(subQueryProjections, ds);
            var visited = visitor.Visit(toFindIn);
            return visited;
        }

        public SubQueryProjectionReplacementVisitor(SelectColumn[] subQueryProjections, AliasedDataSource ds)
        {
            //this.subQueryProjections = subQueryProjections ?? throw new ArgumentNullException(nameof(subQueryProjections));
            this.subQueryDataSource = ds ?? throw new ArgumentNullException(nameof(ds));
            this.subQueryDataSourceAlias = ds.Alias;
            this.hashGenerator = new SqlExpressionHashGenerator();
            this.subQueryProjectionHashMap = subQueryProjections.Select(x => (this.hashGenerator.Generate(x.ColumnExpression), x)).ToList();
        }

        protected internal override SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
        {
            referenceReplaced.Push(new ReferenceReplacementFlag { IsReplaced = false });
            var visitedNode = base.VisitSqlDerivedTable(node) as SqlDerivedTableExpression
                                ??
                                throw new InvalidOperationException($"Expected expression type is '{nameof(SqlDerivedTableExpression)}'.");
            var popped = referenceReplaced.Pop();
            if (popped.IsReplaced && this.visitingCteDataSource.Count > 0 && this.visitingCteDataSource.Peek())
            {
                // it means in the current derived table an outer data source reference was used
                // so we need to see if that data source is a CTE reference we need to add it as cross join
                if (this.subQueryDataSource.QuerySource is SqlCteReferenceExpression sourceCteRef)
                {
                    if (!visitedNode.Joins.Any(x => x.QuerySource is SqlCteReferenceExpression cteRef && cteRef.CteAlias == sourceCteRef.CteAlias))
                    {
                        var cteReference = new SqlCteReferenceExpression(sourceCteRef.CteAlias);
                        var join = new SqlAliasedJoinSourceExpression(SqlJoinType.Cross, cteReference, this.subQueryDataSource.Alias, joinCondition: null, joinName: null, isNavigationJoin: false);
                        var newJoins = visitedNode.Joins.Concat(new[] { join }).ToArray();
                        visitedNode = visitedNode.Update(visitedNode.CteDataSources, visitedNode.FromSource, newJoins, visitedNode.WhereClause, visitedNode.GroupByClause, visitedNode.HavingClause, visitedNode.OrderByClause, visitedNode.SelectColumnCollection);
                    }
                }
            }
            return visitedNode;
        }

        private ReferenceReplacementFlag CurrentFlag => referenceReplaced.Count > 0 ? referenceReplaced.Peek() : null;

        protected internal override SqlExpression VisitSqlAliasedCteSource(SqlAliasedCteSourceExpression node)
        {
            this.visitingCteDataSource.Push(true);
            var visitedNode = base.VisitSqlAliasedCteSource(node);
            this.visitingCteDataSource.Pop();
            return visitedNode;
        }

        private readonly Stack<SqlMemberAssignment> memberAssignmentStack = new Stack<SqlMemberAssignment>();

        protected internal override SqlExpression VisitSqlMemberInit(SqlMemberInitExpression node)
        {
            var newList = new List<SqlMemberAssignment>();
            foreach (var binding in node.Bindings)
            {
                this.memberAssignmentStack.Push(binding);
                var newBinding = Visit(binding.SqlExpression);
                this.memberAssignmentStack.Pop();
                if (newBinding != binding.SqlExpression)
                {
                    newList.Add(new SqlMemberAssignment(binding.MemberName, newBinding));
                }
                else
                {
                    newList.Add(binding);
                }
            }
            return node.Update(newList);
        }

        public override SqlExpression Visit(SqlExpression node)
        {
            if (node is null) return null;
            try
            {
                this.sqlExpressionStack.Push(node);
                var nodeHash = this.hashGenerator.Generate(node);
                if (this.subQueryProjectionHashMap.Where(x => x.Item1 == nodeHash).Any())
                {
                    /*
                        here we are handling the case if inner query is using same expression in 2 columns with different aliases,
                        we want to pick the exact alias, even though it would work but still we want to make query 100% correct
                        e.g.

                     var q = (from e in employees
                                let result1 = e.Name
                                let result2 = e.Department
                                orderby result1, result2
                                select new { result1, result2, e.Name })
                                .Select(x=>new { x.result1, x.result2, x.Name });

                    In above example `x.result1` and `x.Name` both are pointing to same column `Name` in inner query
                    which could lead to it to render something like this

                                                                        this should be a_2.Name
                                                                           _____|_____
                                                                          |           |
                    select a_2.result1 as result1, a_2.result2 as result2, a_2.result1 as Name          
                    from (
                            select a_1.Name as result1, a_1.Department as result2, a_1.Name as Name
                            from Employee as a_1
                            order by a_1.Name asc, a_1.Department asc
                        ) as a_2

                    As we can see it is selecting `a_2.result1 as Name` which is *correct* as far as results are concern but
                    does not look right from LINQ to SQL conversion. 
                    This selection is happening because Hash of inner SqlExpression (a_1.Name) is same, so system is 
                    picking first matched.

                    But below we are checking if multiple expressions are matched and parent is SqlCompositeBindingExpression
                    then match the alias as well.

                     */

                    string parentAlias = this.memberAssignmentStack.Count > 0 ? this.memberAssignmentStack.Peek().MemberName : null;
                    var subQueryProjectionMatched = this.subQueryProjectionHashMap.Where(x => x.Item1 == nodeHash).OrderBy(x => x.Item2.Alias == parentAlias ? 0 : 1).First();
                    if (CurrentFlag != null)
                        CurrentFlag.IsReplaced = true;
                    return new SqlDataSourceColumnExpression(subQueryDataSourceAlias, subQueryProjectionMatched.Item2.Alias);
                }
                return base.Visit(node);
            }
            finally
            {
                this.sqlExpressionStack.Pop();
            }
        }
    }
}
