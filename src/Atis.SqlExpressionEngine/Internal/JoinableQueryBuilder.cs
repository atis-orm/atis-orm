using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using Atis.SqlExpressionEngine.Visitors;
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
        public JoinableQueryBuilderResult(SqlDerivedTableExpression normalizedDerivedTable, SqlExpression joinCondition, IReadOnlyCollection<SqlExpression> otherTablePredicateSides)
        {
            this.NormalizedDerivedTable = normalizedDerivedTable;
            this.JoinCondition = joinCondition;
            this.OtherTablePredicateSides = otherTablePredicateSides;
        }

        public SqlDerivedTableExpression NormalizedDerivedTable { get;  }
        public SqlExpression JoinCondition { get; }
        public IReadOnlyCollection<SqlExpression> OtherTablePredicateSides { get; }
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
            return //this.derivedTable.AutoProjection &&
                   //this.derivedTable.AllDataSources.Count == 1 &&
                   //this.derivedTable.Joins.Length == 0 &&
                   //this.derivedTable.CteDataSources.Length == 0 &&
                    !(this.derivedTable.HavingClause?.FilterConditions.Length > 0) &&
                    this.derivedTable.GroupByClause.Length == 0 &&
                    !(this.derivedTable.OrderByClause?.OrderByColumns.Length > 0) &&
                    this.derivedTable.Top == null &&
                    this.derivedTable.IsDistinct == false &&
                    this.derivedTable.RowOffset == null &&
                    this.derivedTable.RowsPerPage == null;
                    //&&
                    //this.derivedTable.WhereClause?.FilterConditions.Length > 0;
        }

        public bool TryBuild(out JoinableQueryBuilderResult result)
        {
            if (this.IsJoinable())
            {
                return this.TryBuildUnsafe(out result);
            }
            result = null;
            return false;
        }

        public bool TryBuildUnsafe(out JoinableQueryBuilderResult result)
        {
            if (JoinPredicateExtractor.TryExtractJoinPredicates(derivedTable, this.externalDataSourceAliases, out var derivedTableToJoinNormalized, out var joinConditions, out var otherTablePredicateSides))
            {
                SqlExpression joinCondition;
                if (joinConditions?.Count > 0)
                {
                    joinCondition = this.CombinePredicates(joinConditions);

                    var derivedTableProjections = derivedTableToJoinNormalized.SelectColumnCollection.SelectColumns.Where(x => !(x.ColumnExpression is SqlQueryableExpression)).ToArray();
                    // Since SqlDerivedTableExpression has this condition that it's SelectList cannot be null / empty, therefore,
                    // we are guaranteed that we will have at least 1 column in the derivedTable.
                    // This query has been wrapped, so the actual query has been moved inside as derived table,
                    // so the select list expression should have same expressions as sqlExpression
                    var tempDs = new AliasedDataSource(derivedTableToJoinNormalized, this.newDataSourceAlias);
                    joinCondition = SubQueryProjectionReplacementVisitor.FindAndReplace(derivedTableProjections, tempDs, joinCondition);

                    // we cannot do find and replace in otherTablePredicateSides because they will be used internally within the derivedTable
                }
                else
                {
                    joinCondition = null;
                }

                result = new JoinableQueryBuilderResult(derivedTableToJoinNormalized, joinCondition, otherTablePredicateSides);
                return true;
            }
            result = null;
            return false;
        }

        private SqlExpression CombinePredicates(IEnumerable<SqlExpression> predicates)
        {
            var combinedPredicates = predicates.Aggregate((accumulatedExpression, nextPredicate) => this.sqlFactory.CreateBinary(accumulatedExpression, nextPredicate, SqlExpressionType.AndAlso));
            return combinedPredicates;
        }
    }
}
