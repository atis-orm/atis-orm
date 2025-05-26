using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public partial class SqlSelectExpression
    {
        class SqlSelectExpressionCopyMaker
        {
            private readonly SqlSelectExpression sourceSelect;
            private readonly Dictionary<Guid, Guid> aliasMap;

            public SqlSelectExpressionCopyMaker(SqlSelectExpression sourceSelect)
            {
                this.sourceSelect = sourceSelect ?? throw new ArgumentNullException(nameof(sourceSelect));
				var cteMap = this.sourceSelect.CteDataSources.Select(x => new KeyValuePair<Guid, Guid>(x.CteAlias, Guid.NewGuid())).ToArray();
				var dsMap = this.sourceSelect.DataSources.Select(x => new KeyValuePair<Guid, Guid>(x.Alias, Guid.NewGuid())).ToArray();
				this.aliasMap = cteMap.Concat(dsMap).ToDictionary(x => x.Key, x => x.Value);
			}

            private Guid GetNewAlias(Guid oldAlias) => this.aliasMap[oldAlias];

            public static SqlSelectExpression CreateCopy(SqlSelectExpression source)
            {
				if (source is null)
					throw new ArgumentNullException(nameof(source));
				var copyMaker = new SqlSelectExpressionCopyMaker(source);
				var copy = copyMaker.Copy();
				return copy;
			}

            public SqlSelectExpression Copy()
            {
                var cteDataSources = new List<CteDataSource>();
                foreach (var oldCteDataSource in this.sourceSelect.CteDataSources)
                {
                    var newCteBody = this.ReplaceDataSourceAliases(oldCteDataSource.CteBody);
                    var newCte = new CteDataSource(newCteBody, GetNewAlias(oldCteDataSource.CteAlias));
                    cteDataSources.Add(newCte);
                }

                var dataSources = new List<AliasedDataSource>();
                foreach (var oldDataSource in this.sourceSelect.DataSources) 
                {
                    AliasedDataSource newDataSource;
					var newJoinedQuery = this.ReplaceDataSourceAliases(oldDataSource.QuerySource);
                    var newAlias = GetNewAlias(oldDataSource.Alias);
					if (oldDataSource is JoinDataSource oldJoin)
                    {
						var newJoinCondition = this.ReplaceDataSourceAliases(oldJoin.JoinCondition);
						newDataSource = new JoinDataSource(oldJoin.JoinType, newJoinedQuery, newAlias, newJoinCondition, oldJoin.JoinName, oldJoin.IsNavigationJoin);
					}
					else if (oldDataSource is AliasedDataSource oldDs)
					{
						newDataSource = new AliasedDataSource(newJoinedQuery, newAlias);
					}
					else
					{
						throw new NotSupportedException($"Unsupported data source type: {oldDataSource.GetType()}");
					}
					dataSources.Add(newDataSource);
				}

				var selectList = new List<SelectColumn>();
                List<(SqlExpression OldColumnExpression, SqlExpression NewColumnExpression)> selectColumnMap = new List<(SqlExpression, SqlExpression)>();
                foreach (var oldSelectItem in this.sourceSelect.SelectList)
                {
                    var updatedColExpression = this.ReplaceDataSourceAliases(oldSelectItem.ColumnExpression);
                    selectColumnMap.Add((oldSelectItem.ColumnExpression, updatedColExpression));
                    var newSelectCol = new SelectColumn(updatedColExpression, oldSelectItem.Alias, oldSelectItem.ScalarColumn);
                    selectList.Add(newSelectCol);
                }

                var whereClause = new List<FilterCondition>();
                foreach (var oldWhereItem in this.sourceSelect.WhereClause)
                {
                    var updatedPredicate = this.ReplaceDataSourceAliases(oldWhereItem.Predicate);
                    var newWhereClause = new FilterCondition(updatedPredicate, oldWhereItem.UseOrOperator);
                    whereClause.Add(newWhereClause);
                }

                var groupByClause = this.ReplaceDataSourceAliases(this.sourceSelect.GroupByClause);

                var havingClause = new List<FilterCondition>();
                foreach (var oldHavingItem in this.sourceSelect.HavingClause)
                {
                    var updatedPredicate = this.ReplaceDataSourceAliases(oldHavingItem.Predicate);
                    var newHavingClause = new FilterCondition(updatedPredicate, oldHavingItem.UseOrOperator);
                    havingClause.Add(newHavingClause);
                }

                var orderByClause = new List<OrderByColumn>();
                foreach (var oldOrderByItem in this.sourceSelect.OrderByClause)
                {
                    var updatedColumn = this.ReplaceDataSourceAliases(oldOrderByItem.ColumnExpression);
                    var newOrderByCol = new OrderByColumn(updatedColumn, oldOrderByItem.Direction);
                    orderByClause.Add(newOrderByCol);
                }

                var top = this.sourceSelect.Top;
                var isDistinct = this.sourceSelect.IsDistinct;
                var rowOffset = this.sourceSelect.RowOffset;
                var rowsPerPage = this.sourceSelect.RowsPerPage;
                var autoProjection = this.sourceSelect.AutoProjection;
                var tag = this.sourceSelect.Tag;
				var sqlFactory = this.sourceSelect.SqlFactory;

				var queryShape = this.CreateQueryShapeCopy(this.sourceSelect.QueryShape, selectColumnMap);
                                
                var copy = new SqlSelectExpression(cteDataSources, dataSources, selectList, whereClause, groupByClause, havingClause, orderByClause, top, isDistinct, rowOffset, rowsPerPage, autoProjection, tag, queryShape, sqlFactory);
                return copy;
            }

            private SqlExpression CreateQueryShapeCopy(SqlExpression sqlExpression, List<(SqlExpression OldColumnExpression, SqlExpression NewColumnExpression)> selectColumnMap)
            {
                if (sqlExpression is SqlMemberInitExpression queryShape)
                {
                    var bindingList = new List<SqlMemberAssignment>();
                    foreach (var binding in queryShape.Bindings)
                    {
                        var newBinding = new SqlMemberAssignment(binding.MemberName, this.CreateQueryShapeCopy(binding.SqlExpression, selectColumnMap));
                        bindingList.Add(newBinding);
                    }
                    return new SqlMemberInitExpression(bindingList);
                }
                else
                {
                    SqlExpression updatedExpression;
                    if (selectColumnMap.Any(x => x.OldColumnExpression == sqlExpression))
                        updatedExpression = selectColumnMap.First(x => x.OldColumnExpression == sqlExpression).NewColumnExpression;
                    else
                        updatedExpression = this.ReplaceDataSourceAliases(sqlExpression);
                    return updatedExpression;
                }
			}

            public T ReplaceDataSourceAliases<T>(T sqlExpressions) where T : SqlExpression
            {
				var newSqlExpression = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, sqlExpressions);
                return newSqlExpression as T
                        ??
                        throw new InvalidOperationException($"Replace didn't convert the correct type");
			}


			public SqlExpression ReplaceDataSourceAliases(SqlExpression sqlExpressions)
            {
				var newSqlExpression = ReplaceDataSourceAliasVisitor.FindAndReplace(aliasMap, sqlExpressions);
				return newSqlExpression;
			}
        }
    }
}
