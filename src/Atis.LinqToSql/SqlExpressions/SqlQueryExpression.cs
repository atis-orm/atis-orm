using Atis.LinqToSql.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql.SqlExpressions
{
    public enum SqlQueryOperation
    {
        Select,
        Join,
        Where,
        GroupBy,
        OrderBy,
        Top,
        Distinct,
        RowOffset,
        RowsPerPage,
        Union,
    }

    /// <summary>
    ///     <para>
    ///         Represents a SQL query expression.
    ///     </para>
    /// </summary>
    public class SqlQueryExpression : SqlQuerySourceExpression
    {
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Query;

        /// <summary>
        ///     <para>
        ///         Gets the projection of the query.
        ///     </para>
        ///     <para>
        ///         Returns <c>null</c> if no project is applied yet.
        ///     </para>
        /// </summary>
        public SqlExpression Projection { get; protected set; }
        /// <summary>
        ///     <para>
        ///         Gets the Group By part of the query.
        ///     </para>
        ///     <para>
        ///         Returns <c>null</c> if Group By is not applied yet.
        ///     </para>
        /// </summary>
        public SqlExpression GroupBy { get; protected set; }
        /// <summary>
        ///     <para>
        ///         Gets the Top part of the query.
        ///     </para>
        ///     <para>
        ///         Returns <c>null</c> if Top is not applied yet.
        ///     </para>
        /// </summary>
        public SqlExpression Top { get; protected set; }
        /// <summary>
        ///     <para>
        ///         Gets the Row Offset part of the query.
        ///     </para>
        /// </summary>
        public int? RowOffset { get; protected set; }
        /// <summary>
        ///     <para>
        ///         Gets the Rows Per Page part of the query.
        ///     </para>
        /// </summary>
        public int? RowsPerPage { get; protected set; }

        public bool IsDistinct { get; protected set; }

        private readonly List<FilterPredicate> whereClause = new List<FilterPredicate>();
        /// <summary>
        ///     <para>
        ///         Gets list of Where filters applied to the query.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<FilterPredicate> WhereClause => whereClause;
        private readonly List<FilterPredicate> havingClauseList = new List<FilterPredicate>();
        /// <summary>
        ///     <para>
        ///         Gets list of Having filters applied to the query.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<FilterPredicate> HavingClause => havingClauseList;

        private SqlDataSourceExpression[] CombinedDataSources
        {
            get
            {
                // return initialDataSources + joins.DataSource
                return new[] { this.InitialDataSource }.Concat(this.joins.Select(x => x.JoinedSource)).ToArray();
            }
        }
        /// <summary>
        ///     <para>
        ///         Gets the initial data source which was used to create the query.
        ///     </para>
        /// </summary>
        public SqlDataSourceExpression InitialDataSource { get; protected set; }
        /// <summary>
        ///     <para>
        ///         Gets all the data sources used in the query, initial data source and
        ///         all the joined data sources but not CTE Data Sources, however, does return
        ///         CTE references.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlDataSourceExpression> DataSources => CombinedDataSources;
        /// <summary>
        ///     <para>
        ///         Gets both normal data sources and CTE data sources combined.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlDataSourceExpression> AllDataSources => CombinedDataSources.Where(x => !(x.DataSource is SqlCteReferenceExpression)).Concat(this.cteDataSources).ToArray();

        private readonly List<SqlJoinExpression> joins = new List<SqlJoinExpression>();
        /// <summary>
        ///     <para>
        ///         Gets the list of joins applied to the query.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlJoinExpression> Joins => joins;

        private readonly List<SqlOrderByExpression> orderBy = new List<SqlOrderByExpression>();
        /// <summary>
        ///     <para>
        ///         Gets the list of Order By expressions applied to the query.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlOrderByExpression> OrderBy => orderBy;

        private readonly List<SqlUnionExpression> unions = new List<SqlUnionExpression>();
        private readonly List<SqlDataSourceExpression> cteDataSources = new List<SqlDataSourceExpression>();
        /// <summary>
        ///     <para>
        ///         Gets the list of Common Table Expressions (CTEs) used in the query.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlDataSourceExpression> CteDataSources => this.cteDataSources;
        /// <summary>
        ///     <para>
        ///         Gets the list of Union expressions applied to the query.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlUnionExpression> Unions => unions;
        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether the query is a multi-data source query.
        ///     </para>
        /// </summary>
        public bool IsMultiDataSourceQuery => this.CombinedDataSources.Where(x => x is SqlFromSourceExpression).Any();
        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether the query is a Common Table Expression (CTE).
        ///     </para>
        /// </summary>
        public bool IsCte { get; protected set; }
        /// <summary>
        ///    <para>
        ///     Gets a value indicating whether the query has projection applied automatically.
        ///    </para>
        /// </summary>
        public bool AutoProjectionApplied { get; protected set; } = false;
        /// <summary>
        ///     <para>
        ///         Gets the default data source for the query.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <see cref="DefaultDataSource"/> is set when <c>SelectMany</c> is used to select the child navigation property.
        ///         In that case the child navigation is added to the query as a joined data source while we have initial data source
        ///         present. After this change, we cannot use the initial data source as the main data source, so we set this property
        ///         so that we use the default data source when applying auto projection.
        ///     </para>
        /// </remarks>
        public SqlDataSourceExpression DefaultDataSource { get; protected set; }

        /// <summary>
        ///     <para>
        ///         Creates a new instance of the <see cref="SqlQueryExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="dataSource">A data source to be used in the query.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="dataSource"/> is <c>null</c>.</exception>
        public SqlQueryExpression(SqlDataSourceExpression dataSource)
        {
            if (dataSource is null)
                throw new ArgumentNullException(nameof(dataSource));
            this.AddDataSources(dataSource);
        }

        /// <summary>
        ///     <para>
        ///         Creates a new instance of the <see cref="SqlQueryExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="dataSources">List of data sources to be used in the query.</param>
        public SqlQueryExpression(IEnumerable<SqlDataSourceExpression> dataSources)
        {
            this.AddDataSources(dataSources?.ToArray());
        }

        public SqlQueryExpression(Guid cteAlias, SqlQueryExpression cteSource)
        {
            this.ConvertToCte(cteAlias, cteSource);
        }

        /// <summary>
        ///     <para>
        ///         Gets the actual data source by looking at the given <paramref name="usualDataSourceOrCteDataSource"/>.
        ///     </para>
        /// </summary>
        /// <param name="usualDataSourceOrCteDataSource">Data source to be checked.</param>
        /// <returns>Actual data source from the given <paramref name="usualDataSourceOrCteDataSource"/>.</returns>
        /// <remarks>
        ///     <para>
        ///         During the <c>MemberExpression</c> conversion if we receive a data source of which we need to extract
        ///         the column, then this is possible that data source is a <see cref="SqlCteReferenceExpression"/> which
        ///         does not contain any column / query information except it only contains the CTE data source reference.
        ///         Therefore, in that case we pick the CTE Data Source using the Data Source Alias and then we return
        ///         that CTE Data Source.
        ///     </para>
        /// </remarks>
        public SqlDataSourceExpression HandleCteOrUsualDataSource(SqlDataSourceExpression usualDataSourceOrCteDataSource)
        {
            if (usualDataSourceOrCteDataSource.DataSource is SqlCteReferenceExpression cteRef)
            {
                return this.cteDataSources.Where(x => x.DataSourceAlias == cteRef.CteAlias).FirstOrDefault();
            }
            return usualDataSourceOrCteDataSource;
        }

        /// <summary>
        ///     <para>
        ///         Indicates if this query instance is a simple table query with no joins, where clause, group by, order by, top, etc.
        ///     </para>
        /// </summary>
        /// <returns><c>true</c> if this query is a simple table query; otherwise, <c>false</c>.</returns>
        public bool IsTableOnly()
        {
            return this.CombinedDataSources.Length == 1 &&
                        this.CombinedDataSources[0].DataSource is SqlTableExpression &&
                        this.whereClause.Count == 0 &&
                        this.GroupBy == null &&
                        this.havingClauseList.Count == 0 &&
                        // probably this.Projection will never be null
                        (this.Projection == null || this.AutoProjectionApplied) &&
                        this.orderBy.Count == 0 &&
                        this.Top == null &&
                        this.joins.Count == 0 &&
                        this.IsDistinct == false &&
                        this.RowOffset == null &&
                        this.RowsPerPage == null
                        ;
        }

        /// <summary>
        ///     <para>
        ///         Sets the project to literal <c>1</c> for EXISTS query.
        ///     </para>
        /// </summary>
        public void SetProjectionForExists()
        {
            this.Projection = new SqlLiteralExpression(1);
        }

        /// <summary>
        ///     <para>
        ///         Adds a data source to the query.
        ///     </para>
        /// </summary>
        /// <param name="joinedDataSource">A data source to be added to the query.</param>
        /// <remarks>
        ///     <para>
        ///         This method is normally called during the navigation conversion where the
        ///         data source is created but the join expression is yet to be created.
        ///         So we add the data source in the query so that the <c>MemberExpression</c>
        ///         within join condition can be converted to the column expression.
        ///     </para>
        /// </remarks>
        public void AddDataSource(SqlDataSourceExpression joinedDataSource)
        {
            this.AddDataSource(joinedDataSource, checkWrap: true);
        }

        private void AddDataSource(SqlDataSourceExpression joinedDataSource, bool checkWrap)
        {
            if (checkWrap)
                joinedDataSource = this.WrapIfRequired(joinedDataSource, SqlQueryOperation.Join) as SqlDataSourceExpression
                                    ?? throw new InvalidOperationException("Joined data source must be of type SqlDataSourceExpression.");

            this.AddDataSources(joinedDataSource);
        }

        /// <summary>
        ///     <para>
        ///         Adds a child data source as join and also maps it with the parent data source within this query.
        ///     </para>
        /// </summary>
        /// <param name="parentSqlExpression">Parent SQL Expression which can be this query instance or a child data source within this query.</param>
        /// <param name="childDataSourceExpression">A new data source to be added to the query.</param>
        /// <param name="navigationName">Navigation name to identify this child data source.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddJoinedDataSource(SqlExpression parentSqlExpression, SqlDataSourceExpression childDataSourceExpression, string navigationName)
        {
            if (parentSqlExpression is null)
                throw new ArgumentNullException(nameof(parentSqlExpression));
            if (childDataSourceExpression is null)
                throw new ArgumentNullException(nameof(childDataSourceExpression));
            if (childDataSourceExpression.ParentSqlQuery != null)
                throw new InvalidOperationException($"Argument '{nameof(childDataSourceExpression)}' is already a part of another SQL query.");
            if (string.IsNullOrEmpty(navigationName))
                throw new ArgumentNullException(nameof(navigationName));
            if (parentSqlExpression != this)
            {
                // then it must be the child data source within this query
                if (!this.DataSources.Any(x => x == parentSqlExpression))
                    throw new InvalidOperationException($"Argument '{nameof(parentSqlExpression)}' is not a part of Data Source within this SQL query.");
            }
            this.AddDataSource(childDataSourceExpression, checkWrap: this.LastSqlOperation != SqlQueryOperation.Where);
            this.autoJoins.Add(this.joins.Last());
            this.MapDataSourceWithChildDataSource(parentSqlExpression, navigationName, childDataSourceExpression);
        }

        // this collection helps to understand whether the join was added automatically
        // because of navigation, therefore, when applying auto projection we'll
        // check if the navigation which is present as a joined data source in the query
        // was added automatically because of navigation then we will not use it's column
        // when applying auto projection
        private readonly List<SqlJoinExpression> autoJoins = new List<SqlJoinExpression>();

        private void AddDataSources(params SqlDataSourceExpression[] dataSources)
        {
            // CAUTION: use the given dataSources only and do not create a new instance,
            //          because it might break the caller
            //          logic as it will not know that this class is no longer using the
            //          its provided dataSource.
            if (dataSources is null || !dataSources.Any())
                throw new ArgumentNullException(nameof(dataSources));
            foreach (var dataSource in dataSources)
            {
                dataSource.AttachToParentSqlQuery(this);
            }
            var dataSourcesToAdd = dataSources.AsEnumerable();
            if (this.InitialDataSource == null)
            {
                this.InitialDataSource = dataSources[0];
                dataSourcesToAdd = dataSources.Skip(1);
            }
            // by default any data source that is added to the query will be added
            // as Cross Join
            this.joins.AddRange(dataSourcesToAdd.Select(x => new SqlJoinExpression(SqlJoinType.Cross, x, joinCondition: null)));
        }

        /// <summary>
        ///     <para>
        ///         Clears the currently applied auto projection.
        ///     </para>
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        ///     <para>
        ///         During the <c>SelectMany</c> call, the child query is usually closed after conversion,
        ///         therefore, it will have Auto Projection applied to it. However, <c>SelectMany</c> method
        ///         also supports custom projection as parameter, in that case we will be needing to remove
        ///         the auto applied projection and apply projection that is coming in the next <c>SelectMany</c>
        ///         parameter.
        ///     </para>
        /// </remarks>
        public void ClearAutoProjection()
        {
            if (!this.AutoProjectionApplied)
                throw new InvalidOperationException($"Current projection is not auto, cannot remove.");
            this.UndoProjection();
        }

        public void UndoProjection()
        {
            if (this.LastSqlOperation == SqlQueryOperation.Select && this.lastMethodBeforeSelect != null)
            {
                this.LastSqlOperation = this.lastMethodBeforeSelect;        // variable is used
                this.lastMethodBeforeSelect = null;                         // reset the variable
            }
            this.Projection = null;
            this.AutoProjectionApplied = false;
        }

        public void ApplyDistinct()
        {
            this.WrapIfRequired(null, SqlQueryOperation.Distinct);
            
            this.IsDistinct = true;
        }

        /// <summary>
        ///     <para>
        ///         Applies the projection to the query.
        ///     </para>
        /// </summary>
        /// <param name="selector">Sql Expression to be used as projection.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ApplyProjection(SqlExpression selector)
        {
            selector = this.WrapIfRequired(selector, SqlQueryOperation.Select);

            SqlExpression projection;
            if (selector is SqlDataSourceReferenceExpression dsReference)
            {
                var columns = this.GetColumnsFromDataSourceReference(dsReference, ModelPath.Empty);
                projection = new SqlCollectionExpression(columns);
            }
            else if (selector is SqlCollectionExpression sqlCollection)
            {
                // we are assuming that the collection contains only SqlColumnExpression
                SqlColumnExpression[] columnExpressions;
                try
                {
                    columnExpressions = sqlCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"System was trying to cast the SqlCollectionExpression.SqlExpressions to SqlColumnsExpression[], but an error occurred. Make sure the MemberInitExpression or NewExpression must be converting to SqlCollectionExpression having SqlColumnExpression as inner collection.", ex);
                }
                var columns = new List<SqlColumnExpression>();
                foreach (var colExpr in columnExpressions)
                {
                    if (colExpr.ColumnExpression is SqlDataSourceReferenceExpression dsReference1)
                    {
                        var dsColumns = this.GetColumnsFromDataSourceReference(dsReference1, colExpr.ModelPath);
                        columns.AddRange(dsColumns);
                    }
                    else
                        columns.Add(colExpr);
                }
                projection = new SqlCollectionExpression(columns);
            }
            else
            {
                projection = new SqlColumnExpression(selector, "Col1", ModelPath.Empty, scalar: true);
            }
            this.ApplyProjectionInternal(projection);
        }

        private void ApplyProjectionInternal(SqlExpression projection)
        {
            projection = this.FixDuplicateColumns(projection);
            this.Projection = projection;
            this.dataSourceWithSubDataSourceMap.Clear();
        }

        private SqlExpression FixDuplicateColumns(SqlExpression projection)
        {
            if (projection is SqlCollectionExpression sqlCollection)
            {
                var projectionChanged = false;
                var currentColumns = sqlCollection.SqlExpressions.Cast<SqlColumnExpression>();
                var colDictionary = new Dictionary<string, SqlColumnExpression>();
                foreach (var currentCol in currentColumns)
                {
                    var colToAdd = currentCol;
                    string columnAlias = currentCol.ColumnAlias;
                    if (colDictionary.ContainsKey(currentCol.ColumnAlias))
                    {
                        columnAlias = this.GenerateUniqueColumnAlias(colDictionary, currentCol.ColumnAlias);
                        colToAdd = new SqlColumnExpression(currentCol.ColumnExpression, columnAlias, currentCol.ModelPath);
                        projectionChanged = true;
                    }
                    colDictionary.Add(columnAlias, colToAdd);
                }
                if (projectionChanged)
                    projection = new SqlCollectionExpression(colDictionary.Values.ToArray());
            }

            return projection;
        }

        private string GenerateUniqueColumnAlias(Dictionary<string, SqlColumnExpression> columns, string columnAlias)
        {
            int i = 1;
            var newColumnAlias = $"{columnAlias}_{i}";
            while (columns.ContainsKey(newColumnAlias))
            {
                i++;
                newColumnAlias = $"{columnAlias}_{i}";
            }
            return newColumnAlias;
        }

        /// <summary>
        ///     <para>
        ///         Applies where condition to the query.
        ///     </para>
        /// </summary>
        /// <param name="whereCondition">A SQL expression to be used as where condition.</param>
        public void ApplyWhere(SqlExpression whereCondition)
        {
            this.ApplyWhere(whereCondition, useOrOperator: false);
        }

        /// <summary>
        ///     <para>
        ///         Applies where condition to the query with optional indication to use
        ///         OR operator instead of default AND operator.
        ///     </para>
        /// </summary>
        /// <param name="whereCondition">A SQL expression to be used as where condition.</param>
        /// <param name="useOrOperator">if <c>true</c> then OR operator will be used; otherwise, AND operator will be used.</param>
        public void ApplyWhere(SqlExpression whereCondition, bool useOrOperator)
        {
            whereCondition = this.WrapIfRequired(whereCondition, SqlQueryOperation.Where);

            if (this.GroupBy == null)
                this.whereClause.Add(new FilterPredicate { Predicate = whereCondition, UseOrOperator = useOrOperator });
            else
                this.havingClauseList.Add(new FilterPredicate { Predicate = whereCondition, UseOrOperator = useOrOperator });
        }

        /// <summary>
        ///     <para>
        ///         Applies Join to the query.
        ///     </para>
        /// </summary>
        /// <param name="joinExpression">A SQL join expression to be applied to the query.</param>
        /// <remarks>
        ///     <para>
        ///         If the <paramref name="joinExpression"/>.JoinedDataSource has not already been added, then it adds it,
        ///         note that while adding this data source, if it's already a part of another SQL query then 
        ///         this method will give an error.
        ///     </para>
        /// </remarks>
        public void ApplyJoin(SqlJoinExpression joinExpression)
        {
            // And if the joinExpression.JoinDataSource is already added, then we are
            // sure that it is already a child of this query with correct ParentQuery set.

            var join = this.joins.Where(x => x.JoinedSource == joinExpression.JoinedSource).FirstOrDefault();
            if (join is null)
            {
                // TODO: check if we would every land here

                joinExpression = this.WrapIfRequired(joinExpression, SqlQueryOperation.Join) as SqlJoinExpression
                                ?? throw new InvalidOperationException("Join expression must be of type SqlJoinExpression.");

                if (joinExpression.JoinedSource.ParentSqlQuery == null)
                    joinExpression.JoinedSource.AttachToParentSqlQuery(this);
                this.joins.Add(joinExpression);
            }
            else
            {
                var index = this.joins.IndexOf(join);
                var autoJoin = false;
                if (this.autoJoins.Contains(this.joins[index]))
                {
                    this.autoJoins.Remove(this.joins[index]);
                    autoJoin = true;
                }
                this.joins[index] = joinExpression;
                if (autoJoin)
                    this.autoJoins.Add(joinExpression);
            }
        }

        /// <summary>
        ///     <para>
        ///         Gets the expression which represents a scalar value.
        ///     </para>
        /// </summary>
        /// <returns>SQL Expression representing a scalar value or <c>null</c> in case of no scalar value.</returns>
        /// <remarks>
        ///     <para>
        ///         In case if projection is applied and the applied projection is Scalar Column
        ///         then this method will return the column expression from the scalar column expression.
        ///     </para>
        ///     <para>
        ///         In case if there is a sub-query present within this query and the sub-query has a scalar column
        ///         then this method will return the column expression from the scalar column expression.
        ///     </para>
        ///     <para>
        ///         If none of the above conditions are met then this method will return <c>null</c>.
        ///     </para>
        /// </remarks>
        public SqlExpression GetScalarColumnExpression()
        {
            if (this.Projection.TryGetScalarColumn(out var scalarCol))
            {
                return scalarCol.ColumnExpression;
            }
            else if (this.CombinedDataSources.FirstOrDefault()?.DataSource is SqlQueryExpression subQuery && 
                        subQuery.Projection.TryGetScalarColumn(out var subQueryScalarCol))
            {
                return new SqlDataSourceColumnExpression(this.CombinedDataSources[0], subQueryScalarCol.ColumnAlias);
            }
            // TODO: check if we need to cater the case of DefaultDataSource
            return null;
        }

        /// <summary>
        ///     <para>
        ///         Wraps this query in a sub-query.
        ///     </para>
        /// </summary>
        /// <param name="applyAll"></param>
        /// <remarks>
        ///     <para>
        ///         This method checks if projection has been applied, if not then it applies auto projection 
        ///         to this query. Then it creates a copy of this query and sets this query as outer query
        ///         by re-initializing all the properties and then sets the copy as inner query.
        ///     </para>
        ///     <para>
        ///         Important: the design decision to *not* return the new query instance, instead initialize
        ///         this instance is because this query instance will be used in different places, if we return a 
        ///         new instance to the caller then it will be difficult to caller to replace this new instance
        ///         everywhere.
        ///     </para>
        /// </remarks>
        // TODO: probably we need to remove the uniqueColumns parameter, because it's always passed as true
        public virtual void WrapInSubQuery()
        {
            ApplyAutoProjectionInternal(applyAll: true);

            var copy = CreateCopy();

            // here a new alias will be generated for sub-query
            var newDataSource = new SqlDataSourceExpression(dataSource: copy);
            this.Initialize(newDataSource);
        }

        private void Initialize(SqlDataSourceExpression newDataSource)
        {
            //this.dataSources.Clear();
            this.cteDataSources.Clear();
            this.InitialDataSource = null;
            this.joins.Clear();
            //this important to initialize the InitializeDataSource and joins before calling AddDataSource method
            this.AddDataSources(newDataSource);
            this.Projection = null;
            this.AutoProjectionApplied = false;
            this.whereClause.Clear();
            this.havingClauseList.Clear();
            this.GroupBy = null;
            this.Top = null;
            this.orderBy.Clear();
            this.RowOffset = null;
            this.RowsPerPage = null;
            this.unions.Clear();
            this.IsCte = false;
            this.DefaultDataSource = null;
            this.dataSourceWithSubDataSourceMap.Clear();
            this.autoJoins.Clear();
        }

        /// <summary>
        ///     <para>
        ///         Creates of copy of <paramref name="source"/>.
        ///     </para>
        /// </summary>
        /// <param name="source">A SQL query expression to be copied.</param>
        /// <returns>A new instance of <see cref="SqlQueryExpression"/> with copied properties.</returns>
        public static SqlQueryExpression CreateCopy(SqlQueryExpression source)
        {
            // We are creating new data source, because we don't allow SqlDatSourceExpression ParentQuery to be
            // changed, that's why we are creating fresh SqlDataSourceExpression so that it can be attached with
            // copy of this query.
            // The mapping will be used in the Join so that Join can replace the newly created data sources with
            // the old data sources.
            //var dataSourceMapping = source.dataSources
            //                                .Select(x => new { oldDataSource = x, newDataSource = new SqlDataSourceExpression(x.DataSourceAlias, x.DataSource) })
            //                                .ToDictionary(x => x.oldDataSource, x => x.newDataSource);
            //var dataSourceCopies = dataSourceMapping.Values;
            // IMPORTANT: We cannot just copy the joins, because joins are using DataSources, and we are creating new data sources above
            //              so we need to create new joins with new data sources.
            var initialDataSourceCopy = new SqlDataSourceExpression(source.InitialDataSource);

            var copy = new SqlQueryExpression(initialDataSourceCopy);   // initialDataSourceCopy.ParentSqlQuery will be set here
            copy.Projection = source.Projection;
            copy.whereClause.AddRange(source.whereClause);
            copy.havingClauseList.AddRange(source.havingClauseList);

            // we want to reattach the joins' DataSource with this new copy
            var newJoinDataSources = source.joins.Select(x => new { OldDs = x.JoinedSource, NewDs = new SqlDataSourceExpression(x.JoinedSource) })
                                                    .ToDictionary(x => x.OldDs, x => x.NewDs);
            var oldVsNewDataSource = new Dictionary<SqlDataSourceExpression, SqlDataSourceExpression>(newJoinDataSources)
            {
                { source.InitialDataSource, initialDataSourceCopy }
            };

            var joinsCopy = source.joins.Select(x => new SqlJoinExpression(x.JoinType, newJoinDataSources[x.JoinedSource], x.JoinCondition));
            foreach (var join in joinsCopy)
                join.JoinedSource.AttachToParentSqlQuery(copy);
            copy.joins.AddRange(joinsCopy);

            copy.GroupBy = source.GroupBy;
            copy.Top = source.Top;
            copy.orderBy.AddRange(source.orderBy);
            copy.RowOffset = source.RowOffset;
            copy.RowsPerPage = source.RowsPerPage;
            copy.unions.AddRange(source.Unions);
            copy.IsCte = source.IsCte;
            copy.cteDataSources.AddRange(source.cteDataSources);
            copy.DefaultDataSource = source.DefaultDataSource;
            copy.AutoProjectionApplied = source.AutoProjectionApplied;

            UpdateDataSourcesInChildDataSourceIdentifierMap(source, copy, oldVsNewDataSource);

            return copy;
        }

        private static void UpdateDataSourcesInChildDataSourceIdentifierMap(SqlQueryExpression source, SqlQueryExpression copy, Dictionary<SqlDataSourceExpression, SqlDataSourceExpression> oldVsNewDataSource)
        {
            foreach (var kv in source.dataSourceWithSubDataSourceMap)
            {
                var oldParentExpression = kv.Key.ParentExpression;
                var oldChild = kv.Value;

                // if source.DataSourceIdentifierMap.ParentExpression = source (means the parent was the query)
                //  then we'll pick the `copy` as new parent (new query)
                // otherwise, it means ParentExpression was a SqlDataSourceExpression so we pick the corresponding newly created data source
                var newParentExpression = oldParentExpression == source ? copy : (SqlExpression)oldVsNewDataSource[(SqlDataSourceExpression)oldParentExpression];
                var newChild = oldVsNewDataSource[oldChild];

                var newIdentifier = new DataSourceIdentifier(kv.Key.Identifier, newParentExpression);
                copy.dataSourceWithSubDataSourceMap.Add(newIdentifier, newChild);
            }
        }

        /// <summary>
        ///     <para>
        ///         Maps the <paramref name="parentSqlExpression"/> expression with <paramref name="childDataSource"/>
        ///         using <paramref name="identifier"/>.
        ///     </para>
        /// </summary>
        /// <param name="parentSqlExpression">Parent SQL expression to which the child data source is mapped, can be this query instance or a child Data Source.</param>
        /// <param name="identifier">Identifier to be used for mapping.</param>
        /// <param name="childDataSource">Child Data Source within this instance.</param>
        /// <remarks>
        ///     <para>
        ///         During Navigation Property conversion, we add the child data source to this query as a joined data source.
        ///         Since navigation properties can be used more than once within single query, therefore, this mapping helps
        ///         us identifying if the joined source has already been added to the query. So that we don't create redundant
        ///         joins.
        ///     </para>
        /// </remarks>
        protected void MapDataSourceWithChildDataSource(SqlExpression parentSqlExpression, string identifier, SqlDataSourceExpression childDataSource)
        {
            var dataSourceIdentifier = new DataSourceIdentifier(identifier, parentSqlExpression);
            this.dataSourceWithSubDataSourceMap[dataSourceIdentifier] = childDataSource;
        }

        /// <summary>
        ///     <para>
        ///         Tries to get the child data source by the given <paramref name="identifier"/> and <paramref name="parentSqlExpression"/>.
        ///     </para>
        /// </summary>
        /// <param name="parentSqlExpression">Parent SQL expression to which the child data source is mapped, can be this query instance or a child Data Source.</param>
        /// <param name="identifier">Identifier used during mapping.</param>
        /// <param name="childDataSource">Child Data Source within this instance.</param>
        /// <returns><c>true</c> if the child data source is found; otherwise, <c>false</c>.</returns>
        public bool TryGetDataSourceChildByIdentifier(SqlExpression parentSqlExpression, string identifier, out SqlDataSourceExpression childDataSource)
        {
            var dataSourceIdentifier = new DataSourceIdentifier(identifier, parentSqlExpression);
            return this.dataSourceWithSubDataSourceMap.TryGetValue(dataSourceIdentifier, out childDataSource);
        }

        private readonly Dictionary<DataSourceIdentifier, SqlDataSourceExpression> dataSourceWithSubDataSourceMap = new Dictionary<DataSourceIdentifier, SqlDataSourceExpression>();

        private readonly struct DataSourceIdentifier
        {
            public string Identifier { get; }
            public SqlExpression ParentExpression { get; }

            public DataSourceIdentifier(string identifier, SqlExpression parentExpression)
            {
                this.Identifier = identifier;
                this.ParentExpression = parentExpression;
            }

            public override bool Equals(object obj)
            {
                return obj is DataSourceIdentifier child &&
                    child.Identifier == this.Identifier &&
                    child.ParentExpression == this.ParentExpression;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.Identifier, this.ParentExpression);
            }
        }

        /// <summary>
        ///     <para>
        ///         Creates a copy of this query.
        ///     </para>
        /// </summary>
        /// <returns>A new instance of <see cref="SqlQueryExpression"/> with copied properties.</returns>
        public virtual SqlQueryExpression CreateCopy()
        {
            var copy = CreateCopy(this);
            return copy;
        }

        /// <summary>
        ///     <para>
        ///         Applies auto projection to the query.
        ///     </para>
        /// </summary>
        public void ApplyAutoProjection()
        {
            this.ApplyAutoProjectionInternal(applyAll: false);
        }

        private void ApplyAutoProjectionInternal(bool applyAll)
        {
            if (this.Projection == null)
            {
                List<SqlColumnExpression> autoProjection = this.GetAutoProjection(applyAll);
                var projectionExpression = new SqlCollectionExpression(autoProjection);
                this.ApplyProjectionInternal(projectionExpression);
                this.AutoProjectionApplied = true;
            }
        }

        private List<SqlColumnExpression> GetAutoProjection(bool applyAll)
        {
            var autoProjection = new List<SqlColumnExpression>();
            // DefaultDataSource will be set in-case of SelectMany(e => e.ChildNavigation)
            // Since ChildNavigation will be added as InnerJoin so the main query will have all the data sources
            // present.
            var dataSourcesToUse = this.DefaultDataSource != null ? new[] { this.DefaultDataSource } : this.CombinedDataSources;
            foreach (var dataSource in dataSourcesToUse)
            {
                var ds = dataSource;
                if (!applyAll && this.IsAutoAddedNavigationDataSource(ds))
                    continue;

                //var modelMap = ds.ModelMap == null ? "" : $"{ds.ModelMap}.";
                if (ds.DataSource is SqlCteReferenceExpression cteRef)
                {
                    ds = this.cteDataSources.Where(x => x.DataSourceAlias == cteRef.CteAlias).FirstOrDefault()
                                ??
                                throw new InvalidOperationException($"CTE data source not found for alias '{cteRef.CteAlias}'.");
                }
                if (ds.DataSource is SqlTableExpression table)
                {
                    foreach (var tableColumn in table.TableColumns)
                    {
                        var columnAlias = tableColumn.ModelPropertyName;
                        var dataSourceColExpression = new SqlDataSourceColumnExpression(ds, tableColumn.DatabaseColumnName);
                        var sqlColExpression = new SqlColumnExpression(dataSourceColExpression, tableColumn.ModelPropertyName, ds.ModelPath.Append(tableColumn.ModelPropertyName));
                        autoProjection.Add(sqlColExpression);
                    }
                }
                else if (ds.DataSource is SqlQueryExpression subQuery)
                {
                    if (subQuery.Projection is null)
                        throw new InvalidOperationException($"Sub-query does not have a projection set.");
                    if (subQuery.Projection is SqlCollectionExpression sqlCollection)
                    {
                        foreach (var sqlExpression in sqlCollection.SqlExpressions)
                        {
                            if (sqlExpression is SqlColumnExpression sqlColumn)
                            {
                                var columnAlias = sqlColumn.ColumnAlias;
                                var dataSourceColExpression = new SqlDataSourceColumnExpression(ds, sqlColumn.ColumnAlias);
                                var newSqlColumnExpression = new SqlColumnExpression(dataSourceColExpression, sqlColumn.ColumnAlias, ds.ModelPath.Append(sqlColumn.ColumnAlias)/* sqlColumn.ModelMap*/);
                                autoProjection.Add(newSqlColumnExpression);
                            }
                            else
                                throw new InvalidOperationException($"Unexpected expression type in sub-query projection: {sqlExpression.NodeType}");
                        }
                    }
                    else if (subQuery.Projection.TryGetScalarColumn(out var scalarCol))
                    {
                        var columnAlias = scalarCol.ColumnAlias;
                        var dataSourceColExpression = new SqlDataSourceColumnExpression(ds, columnAlias);
                        var newSqlColumnExpression = new SqlColumnExpression(dataSourceColExpression, columnAlias, ds.ModelPath.Append(scalarCol.ColumnAlias) /*scalarCol.ModelMap.AddPrefix(ds.ModelMap)*/);
                        autoProjection.Add(newSqlColumnExpression);
                    }
                    else
                        throw new InvalidOperationException($"Unexpected expression type in sub-query projection: {subQuery.Projection.NodeType}");
                }
            }

            return autoProjection;
        }

        private bool IsAutoAddedNavigationDataSource(SqlDataSourceExpression ds)
        {
            var join = this.joins.Where(x => x.JoinedSource == ds).FirstOrDefault();
            return join != null && 
                        this.autoJoins.Contains(join);
        }

        /// <summary>
        ///     <para>
        ///         Gets the SQL string representation of the query.
        ///     </para>
        ///     <para>
        ///         Caution: this method is not intended to be used for SQL generation, it's only for debugging purposes.
        ///     </para>
        /// </summary>
        /// <returns>A string representing the SQL query.</returns>
        public override string ToString()
        {
            if (this.IsTableOnly())
            {
                return $"TableOnly-Query: {(this.InitialDataSource.DataSource as SqlTableExpression).TableName}";
            }
            else
            {
                var dataSourcesAliases = string.Join(", ", this.AllDataSources.Select(x => DebugAliasGenerator.GetAlias(x)));
                return $"{(this.IsCte ? "Cte-Query" : "Sql-Query")}: {dataSourcesAliases}";
            }
        }

        /// <summary>
        ///     <para>
        ///         Applies Group By to the query.
        ///     </para>
        /// </summary>
        /// <param name="groupByExpression">A SQL expression to be used as Group By.</param>
        /// <exception cref="InvalidOperationException">Throws when Group By is already set.</exception>
        public void ApplyGroupBy(SqlExpression groupByExpression)
        {
            if (this.GroupBy != null)
                throw new InvalidOperationException("GroupBy already set.");
            groupByExpression = this.WrapIfRequired(groupByExpression, SqlQueryOperation.GroupBy);
            this.GroupBy = groupByExpression;
        }

        /// <summary>
        ///     <para>
        ///         Applies Order By to the query.
        ///     </para>
        /// </summary>
        /// <param name="orderByExpression">A SQL Order By expression to be applied to the query.</param>
        public void ApplyOrderBy(SqlOrderByExpression orderByExpression)
        {
            orderByExpression = this.WrapIfRequired(orderByExpression, SqlQueryOperation.OrderBy) as SqlOrderByExpression
                                ?? throw new InvalidOperationException("Order By expression must be of type SqlOrderByExpression.");

            if (this.Projection != null)
            {
                if (this.Projection is SqlCollectionExpression sqlCollection)
                {
                    var matchedExpression = sqlCollection
                                                .SqlExpressions
                                                .Select(x => x as SqlColumnExpression)
                                                .Where(x => x != null)
                                                .Where(x => x.ColumnExpression == orderByExpression.Expression)
                                                .FirstOrDefault();
                    if (matchedExpression != null)
                        orderByExpression = new SqlOrderByExpression(new SqlAliasExpression(matchedExpression.ColumnAlias), orderByExpression.Ascending);
                }
                else if (this.Projection.TryGetScalarColumn(out var scalarCol) && scalarCol.ColumnExpression == orderByExpression.Expression)
                {
                    orderByExpression = new SqlOrderByExpression(new SqlAliasExpression(scalarCol.ColumnAlias), orderByExpression.Ascending);
                }
            }
            this.orderBy.Add(orderByExpression);
        }

        /// <summary>
        ///     <para>
        ///         Applies Top to the query.
        ///     </para>
        /// </summary>
        /// <param name="topExpression">A SQL expression to be used as Top.</param>
        /// <exception cref="InvalidOperationException">Throws when Top is already set.</exception>
        public void ApplyTop(SqlExpression topExpression)
        {
            if (this.Top != null)
                throw new InvalidOperationException("Top already set.");

            topExpression = this.WrapIfRequired(topExpression, SqlQueryOperation.Top);

            this.Top = topExpression;
        }

        /// <summary>
        ///     <para>
        ///         Gets a scalar value of Group By if Group By is on a single column.
        ///     </para>
        /// </summary>
        /// <returns>A scalar expression representing the Group By column; otherwise, <c>null</c>.</returns>
        public SqlExpression GetGroupByScalarExpression()
        {
            if (!(this.GroupBy is SqlCollectionExpression))
            {
                return this.GroupBy;
            }
            return null;
        }

        /// <summary>
        ///     <para>
        ///         Gets the Group By expression by the given <paramref name="name"/>.
        ///     </para>
        /// </summary>
        /// <param name="name">A unique name of the Group By expression.</param>
        /// <returns>Group By expression by the given <paramref name="name"/>; otherwise, <c>null</c>.</returns>
        /// <remarks>
        ///     <para>
        ///         Group by is applied through <c>GroupBy</c> LINQ method conversion. In case if Group By was
        ///         performed using Anonymous Type, then the Group By expression will be a collection of 
        ///         columns.
        ///     </para>
        ///     <para>
        ///         When group by member is accessed using <c>Key</c> later in the query, this method is
        ///         used to get the actual column expression from the Group By collection.
        ///     </para>
        /// </remarks>
        public SqlExpression GetGroupByExpression(string name)
        {
            if (this.GroupBy is SqlCollectionExpression sqlCollection)
            {
                foreach (var sqlExpression in sqlCollection.SqlExpressions)
                {
                    if (sqlExpression is SqlColumnExpression sqlColumn && sqlColumn.ColumnAlias == name)
                    {
                        return sqlColumn.ColumnExpression;
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///     <para>
        ///         Applies Row Offset to the query.
        ///     </para>
        /// </summary>
        /// <param name="rowOffset">Number of rows to skip.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws when <paramref name="rowOffset"/> is less than 1.</exception>
        public void ApplyRowOffset(int rowOffset)
        {
            if (rowOffset < 1)
                throw new ArgumentOutOfRangeException(nameof(rowOffset), rowOffset, "Row Offset must be greater than 0.");

            this.WrapIfRequired(null, SqlQueryOperation.RowOffset);

            this.RowOffset = rowOffset;
        }

        /// <summary>
        ///     <para>
        ///         Applies Rows Per Page to the query.
        ///     </para>
        /// </summary>
        /// <param name="rowsPerPage">Number of rows per page.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws when <paramref name="rowsPerPage"/> is less than 1.</exception>
        /// <remarks>
        ///     <para>
        ///         Applies <c>order by 1</c> to the query if it's not already applied.
        ///     </para>
        /// </remarks>
        public void ApplyRowsPerPage(int rowsPerPage)
        {
            if (rowsPerPage < 1)
                throw new ArgumentOutOfRangeException(nameof(rowsPerPage), rowsPerPage, "Rows per page must be greater than 0.");

            this.WrapIfRequired(null, SqlQueryOperation.RowsPerPage);

            this.RowsPerPage = rowsPerPage;
            if (this.orderBy.Count == 0)
                this.orderBy.Add(new SqlOrderByExpression(new SqlLiteralExpression(1), true));
        }

        /// <summary>
        ///     <para>
        ///         Applies Paging to the query.
        ///     </para>
        /// </summary>
        /// <param name="pageNumber">Page number to be fetched.</param>
        /// <param name="pageSize">Rows per page.</param>
        /// <exception cref="InvalidOperationException">Throws when Paging is already set.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws when <paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than 1.</exception>
        /// <remarks>
        ///     <para>
        ///         Usually paging is applied using <c>Skip</c> and <c>Take</c> LINQ methods. However,
        ///         Atis ORM has <see cref="QueryExtensions.Paging{T}(IQueryable{T}, int, int)"/> method which
        ///         eventually uses this method to apply paging to the query.
        ///     </para>
        /// </remarks>
        public void ApplyPaging(int pageNumber, int pageSize)
        {
            if (this.RowOffset != null)
                throw new InvalidOperationException("Paging already set.");
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "Page number must be greater than 0.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than 0.");

            this.WrapIfRequired(null, SqlQueryOperation.RowsPerPage);

            this.RowOffset = (pageNumber - 1) * pageSize;
            this.RowsPerPage = pageSize;
            if (this.orderBy.Count == 0)
                this.orderBy.Add(new SqlOrderByExpression(new SqlLiteralExpression(1), true));
        }

        /// <summary>
        ///     <para>
        ///         Updates the query with the given parameters.
        ///     </para>
        /// </summary>
        /// <param name="initialDataSource">New initial data source for the query.</param>
        /// <param name="joins">New joins to be applied to the query.</param>
        /// <param name="whereClause">New where clause to be applied to the query.</param>
        /// <param name="groupBy">New group by to be applied to the query.</param>
        /// <param name="projection">New projection to be applied to the query.</param>
        /// <param name="orderByClause">New order by to be applied to the query.</param>
        /// <param name="top">New top to be applied to the query.</param>
        /// <param name="rowsPerPage">New rows per page to be applied to the query.</param>
        /// <param name="rowOffset">New row offset to be applied to the query.</param>
        /// <param name="cteDataSources">New CTE data sources to be applied to the query.</param>
        /// <param name="isCte">New CTE flag to be applied to the query.</param>
        /// <param name="havingClause">New having clause to be applied to the query.</param>
        /// <returns>A new instance of <see cref="SqlQueryExpression"/> with updated properties.</returns>
        public SqlQueryExpression Update(SqlDataSourceExpression initialDataSource, IEnumerable<SqlJoinExpression> joins, IEnumerable<FilterPredicate> whereClause, SqlExpression groupBy, SqlExpression projection, IEnumerable<SqlOrderByExpression> orderByClause, SqlExpression top, IEnumerable<SqlDataSourceExpression> cteDataSources, IEnumerable<FilterPredicate> havingClause, IEnumerable<SqlUnionExpression> unions)
        {
            if (this.InitialDataSource == initialDataSource && this.joins?.SequenceEqual(joins) == true && this.whereClause?.SequenceEqual(whereClause) == true && this.GroupBy == groupBy && this.Projection == projection && this.orderBy?.SequenceEqual(orderByClause) == true && this.Top == top && this.cteDataSources?.SequenceEqual(cteDataSources) == true && this.havingClauseList.SequenceEqual(havingClause) && this.unions?.SequenceEqual(unions) == true)
                return this;
            //var dataSourcesCopy = initialDataSource.Select(x => new SqlDataSourceExpression(x.DataSourceAlias, x.DataSource));

            var initialDataSourceCopy = new SqlDataSourceExpression(initialDataSource);
            Dictionary<SqlDataSourceExpression, SqlDataSourceExpression> oldVsNewDataSource = new Dictionary<SqlDataSourceExpression, SqlDataSourceExpression>
            {
                { initialDataSource, initialDataSourceCopy }
            };
            var sqlQuery = new SqlQueryExpression(initialDataSourceCopy);
            foreach (var join in joins)
            {
                // Usually when we create a data source we don't attach it immediately to any query
                // later when the data source is added to sql query that's when it's attached to a sql query
                // and it's ParentSqlQuery property is set.
                // Since this is Update method, where `joins` parameter can be some join which are just created
                // and are not yet added to a query and supplied to this Update method, in this case
                // the JoinedSource of join will not be attached to any sql query that's what we are
                // testing here and attaching it to the newly created sql query.
                if (join.JoinedSource.ParentSqlQuery == null)
                {
                    join.JoinedSource.AttachToParentSqlQuery(sqlQuery);
                    sqlQuery.joins.Add(join);
                    oldVsNewDataSource.Add(join.JoinedSource, join.JoinedSource);
                }
                else
                {
                    var newJoin = new SqlJoinExpression(join.JoinType, new SqlDataSourceExpression(join.JoinedSource), join.JoinCondition);
                    newJoin.JoinedSource.AttachToParentSqlQuery(sqlQuery);
                    sqlQuery.joins.Add(newJoin);
                    oldVsNewDataSource.Add(join.JoinedSource, newJoin.JoinedSource);
                }
            }
            sqlQuery.whereClause.AddRange(whereClause);
            sqlQuery.havingClauseList.AddRange(havingClause);
            sqlQuery.GroupBy = groupBy;
            sqlQuery.Projection = projection;
            sqlQuery.orderBy.AddRange(orderByClause);
            sqlQuery.Top = top;
            sqlQuery.RowsPerPage = this.RowsPerPage;
            sqlQuery.RowOffset = this.RowOffset;
            foreach (var cteDataSource in cteDataSources)
            {
                SqlDataSourceExpression dsToAdd;
                if (cteDataSource.ParentSqlQuery == null)
                {
                    cteDataSource.AttachToParentSqlQuery(sqlQuery);
                    dsToAdd = cteDataSource;
                }
                else
                    dsToAdd = new SqlDataSourceExpression(cteDataSource);
                sqlQuery.cteDataSources.Add(dsToAdd);
                oldVsNewDataSource.Add(cteDataSource, dsToAdd);
            }

            UpdateDataSourcesInChildDataSourceIdentifierMap(this, sqlQuery, oldVsNewDataSource);
            sqlQuery.unions.AddRange(unions);

            sqlQuery.IsCte = this.IsCte;
            sqlQuery.IsDistinct = this.IsDistinct;
            
            return sqlQuery;
        }

        /// <summary>
        ///     <para>
        ///         Called when visiting the <see cref="SqlQueryExpression"/>.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">Sql Expression Visitor to visit the expression.</param>
        /// <returns>Sql Expression after visiting the expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlQueryExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Applies having condition to the query.
        ///     </para>
        /// </summary>
        /// <param name="predicate">A SQL expression to be used as having condition.</param>
        public virtual void ApplyHaving(SqlExpression predicate)
        {
            this.ApplyHaving(predicate, useOrOperator: false);
        }

        /// <summary>
        ///     <para>
        ///         Applies having condition to the query with optional indication to 
        ///         use OR operator instead of default AND operator.
        ///     </para>
        /// </summary>
        /// <param name="predicate">A SQL expression to be used as having condition.</param>
        /// <param name="useOrOperator">If <c>true</c> then OR operator will be used; otherwise, AND operator will be used.</param>
        public virtual void ApplyHaving(SqlExpression predicate, bool useOrOperator)
        {
            predicate = this.WrapIfRequired(predicate, SqlQueryOperation.Where);
            this.havingClauseList.Add(new FilterPredicate { Predicate = predicate, UseOrOperator = useOrOperator });
        }

        /// <summary>
        ///     <para>
        ///         Applies Union to the query.
        ///     </para>
        /// </summary>
        /// <param name="query">A SQL query expression to be used as Union.</param>
        /// <param name="unionAll">If <c>true</c> then Union All will be applied; otherwise, Union will be applied.</param>
        public virtual void ApplyUnion(SqlQueryExpression query, bool unionAll)
        {
            // will never wrap in union

            this.unions.Add(new SqlUnionExpression(query, unionAll ? SqlExpressionType.UnionAll : SqlExpressionType.Union));
        }

        /// <summary>
        ///     <para>
        ///         Makes the query a Common Table Expression (CTE).
        ///     </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws when query is already a CTE.</exception>
        public void MakeCte()
        {
            if (this.IsCte)
                throw new InvalidOperationException($"Query is already a CTE.");
            this.IsCte = true;
        }

        /// <summary>
        ///     <para>
        ///         Adds a CTE Data Source to the query.
        ///     </para>
        /// </summary>
        /// <param name="sqlDataSourceExpression">A SQL Data Source Expression to be added as CTE Data Source.</param>
        public void AddCteDataSource(SqlDataSourceExpression sqlDataSourceExpression)
        {
            if (sqlDataSourceExpression is null)
                throw new ArgumentNullException(nameof(sqlDataSourceExpression));
            if(sqlDataSourceExpression.NodeType != SqlExpressionType.CteDataSource)
                throw new InvalidOperationException($"sqlDataSourceExpression is not of type CteDataSource.");

            this.IsCte = true;
            // we have a point, where we extract the CTE Data Sources from inner queries and then
            // add them in the outer most queries, at that point, the data source is already attached
            // to a query
            sqlDataSourceExpression.AttachToParentSqlQuery(this);
            this.cteDataSources.Add(sqlDataSourceExpression);
        }


        private IEnumerable<SqlColumnExpression> GetColumnsFromDataSourceReference(SqlDataSourceReferenceExpression dataSourceReference, ModelPath modelPath)
        {
            var dataSource = dataSourceReference.DataSource;
            if (dataSource is SqlDataSourceExpression ds)
            {
                return this.GetColumnsFromDataSource(ds, modelPath);
            }
            else if (dataSource is SqlQueryExpression subQuery)
            {
                return this.GetColumnsFromSqlQuery(subQuery, modelPath);
            }
            else
            {
                throw new InvalidOperationException($"dataSource is not SqlDataSourceExpression or SqlQueryExpression");
            }
        }

        private List<SqlColumnExpression> GetColumnsFromSqlQuery(SqlQueryExpression sqlQueryAsProjection, ModelPath modelPath)
        {
            var columns = new List<SqlColumnExpression>();
            foreach (var ds in sqlQueryAsProjection.DataSources)
            {
                if (this.IsAutoAddedNavigationDataSource(ds))
                    continue;
                var dataSourceColumns = this.GetColumnsFromDataSource(ds, modelPath);
                columns.AddRange(dataSourceColumns);
            }
            return columns;
        }

        private IEnumerable<SqlColumnExpression> GetColumnsFromDataSource(SqlDataSourceExpression ds, ModelPath modelPath)
        {
            var tableOrSubQuery = ds.DataSource;
            List<SqlColumnExpression> columns = new List<SqlColumnExpression>();
            if (tableOrSubQuery is SqlTableExpression table)
            {
                var tableColumns = table.TableColumns.Select(x => new SqlColumnExpression(new SqlDataSourceColumnExpression(ds, x.DatabaseColumnName), x.DatabaseColumnName, modelPath.Append(x.ModelPropertyName)));
                columns.AddRange(tableColumns);
            }
            else if (tableOrSubQuery is SqlQueryExpression subQuery)
            {
                var subQueryColumns = this.GetSubQueryColumns(ds, subQuery, modelPath);
                columns.AddRange(subQueryColumns);
            }
            else
            {
                throw new InvalidOperationException($"tableOrSubQuery is not SqlTableExpression or SqlQueryExpression");
            }
            return columns;
        }

        private IEnumerable<SqlColumnExpression> GetSubQueryColumns(SqlDataSourceExpression ds, SqlQueryExpression subQuery, ModelPath modelPath)
        {
            var subQueryColumns = new List<SqlColumnExpression>();
            var subQueryProjection = subQuery.Projection;
            if (subQueryProjection is SqlCollectionExpression collection)
            {
                foreach (var sqlExpr in collection.SqlExpressions)
                {
                    if (sqlExpr is SqlColumnExpression colExpr)
                    {
                        var colModelPath = modelPath.Append(colExpr.ModelPath);
                        subQueryColumns.Add(new SqlColumnExpression(new SqlDataSourceColumnExpression(ds, colExpr.ColumnAlias), colExpr.ColumnAlias, colModelPath));
                    }
                    else
                        throw new InvalidOperationException($"sqlExpr is not SqlColumnExpression");
                }
            }
            else if (subQueryProjection is SqlColumnExpression colExpr)
            {
                var colModelPath = modelPath.Append(colExpr.ModelPath);
                subQueryColumns.Add(new SqlColumnExpression(new SqlDataSourceColumnExpression(ds, colExpr.ColumnAlias), colExpr.ColumnAlias, colModelPath));
            }
            else
                throw new InvalidOperationException($"subQueryProjection is not SqlCollectionExpression or SqlColumnExpression");
            return subQueryColumns;
        }

        /// <summary>
        ///     <para>
        ///         Sets the last data source as default data source.
        ///     </para>
        /// </summary>
        public void SetLastAsDefaultDataSource()
        {
            this.DefaultDataSource = this.DataSources.Last();
        }

        /// <summary>
        ///     <para>
        ///         Adds a model path prefix to the project or the data sources.
        ///     </para>
        /// </summary>
        /// <param name="modelPathPrefix">Model path prefix to be added.</param>
        public void AddModelPathPrefix(string modelPathPrefix)
        {
            this.AddOrReplaceModelPathPrefix(modelPathPrefix, replace: false);
        }

        /// <summary>
        ///     <para>
        ///         Replaces the model path prefix in the project or the data sources.
        ///     </para>
        /// </summary>
        /// <param name="modelPathPrefix">A new model path prefix to be replaced.</param>
        public void ReplaceModelPathPrefix(string modelPathPrefix)
        {
            this.AddOrReplaceModelPathPrefix(modelPathPrefix, replace: true);
        }

        /// <summary>
        ///     <para>
        ///         Adds or replaces the model path prefix in the project or the data sources.
        ///     </para>
        /// </summary>
        /// <param name="modelPathPrefix">A new model path prefix to be added or replaced.</param>
        /// <param name="replace">If <c>true</c> then the model path prefix will be replaced; otherwise, it will be added.</param>
        public void AddOrReplaceModelPathPrefix(string modelPathPrefix, bool replace)
        {
            if (this.Projection != null)
            {
                if (this.Projection is SqlCollectionExpression sqlCollection)
                {
                    var newCollection = new List<SqlExpression>();
                    foreach (var sqlExpression in sqlCollection.SqlExpressions)
                    {
                        if (sqlExpression is SqlColumnExpression sqlColumn)
                        {
                            var updatedSqlColumn = new SqlColumnExpression(sqlColumn.ColumnExpression, sqlColumn.ColumnAlias, replace ? sqlColumn.ModelPath.ReplaceLastPathEntry(modelPathPrefix) : new ModelPath(modelPathPrefix).Append(sqlColumn.ModelPath));
                            newCollection.Add(updatedSqlColumn);
                        }
                        else
                            newCollection.Add(sqlExpression);
                    }
                    this.Projection = new SqlCollectionExpression(newCollection);
                }
            }
            else
            {
                foreach (var ds in this.CombinedDataSources)
                {
                    ds.AddOrReplaceModelPathPrefix(modelPathPrefix, replace);
                }
                foreach (var ds in this.cteDataSources)
                {
                    ds.AddOrReplaceModelPathPrefix(modelPathPrefix, replace);
                }
            }
        }

        /// <summary>
        ///     <para>
        ///         Updates the join type of the given data source.
        ///     </para>
        /// </summary>
        /// <param name="ds">Joined data source to update the join type.</param>
        /// <param name="joinType">Sql join type to be updated.</param>
        /// <remarks>
        ///     <para>
        ///         As mentioned in <see cref="AddDataSource(SqlDataSourceExpression)"/> method that when
        ///         we add the data source in this class, it is added as Cross Join, so later during
        ///         the conversion process, this method can be used to update the join type.
        ///     </para>
        /// </remarks>
        public void UpdateJoinType(SqlDataSourceExpression ds, SqlJoinType joinType)
        {
            this.joins.Where(x => x.JoinedSource == ds).FirstOrDefault()?.UpdateJoinType(joinType);
            
        }

        /// <summary>
        ///     <para>
        ///         Gets the join type of the given data source.
        ///     </para>
        /// </summary>
        /// <param name="ds">Joined data source to get the join type.</param>
        /// <returns>Sql join type of the given data source; otherwise, <c>null</c>.</returns>
        public SqlJoinType? GetJoinType(SqlDataSourceExpression ds)
        {
            return this.joins.Where(x => x.JoinedSource == ds).FirstOrDefault()?.JoinType;
        }


        // In case of Update command we reverse the Select if it's applied that's why 
        // we have this variable to keep a track of of last operation that was performed
        // before applying the Select, so that we can move back to that part. Otherwise,
        // wrapping might be applied any way.
        // E.g., From(t1 , t2).Join(t1 and t2).Where(filter applied on t1 or t2).Select(new {t1,t2}).Update(t2, t2 fields, filter on t1)
        // In above example, in `Update` method we'll receive `t2` as table to update, but since we have `Select` method applied before
        // that therefore, `t2` will not be converted as DataSource, which we are already handling in 
        // UpdateQueryMethodExpressionConverter, and it automatically selects `t2` as DataSource, 
        // However, the same converter has `Where` application, that's the problematic part,
        // since `Select` has already been applied, therefore, the next `Where` application in converter
        // causes the wrapping of the query which pushes down the `t2` data source and it becomes
        // invalid data source. 
        // Therefore, we undo the projection so that next where does not wrap the query and `t2` becomes
        // valid data source.
        private SqlQueryOperation? lastMethodBeforeSelect;
        protected SqlQueryOperation? LastSqlOperation { get; set; }

        protected SqlExpression WrapIfRequired(SqlExpression expression, SqlQueryOperation newOperation)
        {
            if (expression != null)
                expression = this.CheckAndApplyCteExpression(expression);

            var performWrap = this.IsWrapRequired(newOperation);

            if (performWrap)
            {
                this.WrapInSubQuery();
                if (expression != null)
                {
                    var innerQuery = this.DataSources.First();
                    var innerQueryProjection = (
                                                innerQuery.DataSource as SqlQueryExpression
                                                ??
                                                throw new InvalidOperationException("Wrapped query not found.")
                                                ).Projection;
                    expression = this.ReplaceInnerQueryColumnAccessWithOuterQueryColumnAccess(expression, innerQuery, innerQueryProjection);
                }
            }
            if (newOperation == SqlQueryOperation.Select)
                this.lastMethodBeforeSelect = this.LastSqlOperation;
            this.LastSqlOperation = newOperation;

            return expression;
        }

        public void SetAsNonCte()
        {
            this.IsCte = false;
            this.cteDataSources.Clear();
        }

        protected bool IsWrapRequired(SqlQueryOperation newOperation)
        {
            var performWrap = false;
            switch (this.LastSqlOperation)
            {
                case SqlQueryOperation.Select:
                    performWrap = new SqlQueryOperation[] { SqlQueryOperation.Where, SqlQueryOperation.GroupBy, SqlQueryOperation.Select, SqlQueryOperation.Join }.Contains(newOperation);
                    break;
                case SqlQueryOperation.Where:
                    performWrap = newOperation == SqlQueryOperation.Join;
                    break;
                case SqlQueryOperation.OrderBy:
                    performWrap = new SqlQueryOperation[] { SqlQueryOperation.Where, SqlQueryOperation.GroupBy, SqlQueryOperation.Join }.Contains(newOperation);
                    break;
                case SqlQueryOperation.GroupBy:
                    performWrap = false;
                    break;
                case SqlQueryOperation.Top:
                    performWrap = new SqlQueryOperation[] { SqlQueryOperation.Where, SqlQueryOperation.OrderBy, SqlQueryOperation.Top, SqlQueryOperation.Distinct, SqlQueryOperation.GroupBy, SqlQueryOperation.RowOffset, SqlQueryOperation.RowsPerPage, SqlQueryOperation.Select, SqlQueryOperation.Join }.Contains(newOperation);
                    break;
                case SqlQueryOperation.Distinct:
                    performWrap = new SqlQueryOperation[] { SqlQueryOperation.Where, SqlQueryOperation.OrderBy, SqlQueryOperation.Distinct, SqlQueryOperation.GroupBy, SqlQueryOperation.Select, SqlQueryOperation.Join }.Contains(newOperation); 
                    break;
                case SqlQueryOperation.RowOffset:
                    performWrap = new SqlQueryOperation[] { SqlQueryOperation.Where, SqlQueryOperation.OrderBy, SqlQueryOperation.Top, SqlQueryOperation.Distinct, SqlQueryOperation.GroupBy, SqlQueryOperation.RowOffset,  SqlQueryOperation.Select, SqlQueryOperation.Join }.Contains(newOperation);
                    break;
                case SqlQueryOperation.RowsPerPage:
                    performWrap = new SqlQueryOperation[] { SqlQueryOperation.Where, SqlQueryOperation.OrderBy, SqlQueryOperation.Top, SqlQueryOperation.Distinct, SqlQueryOperation.GroupBy, SqlQueryOperation.RowOffset, SqlQueryOperation.RowsPerPage, SqlQueryOperation.Select, SqlQueryOperation.Join }.Contains(newOperation);
                    break;
                case SqlQueryOperation.Union:
                    performWrap = true;
                    break;
            }
            return performWrap;
        }

        private SqlExpression CheckAndApplyCteExpression(SqlExpression sqlExpression)
        {
            if (!this.IsCte)
            {
                var hasCteDataSource = CteDataSourceSearchVisitor.Find(this, sqlExpression);
                if (hasCteDataSource)
                {
                    this.ApplyAutoProjection();

                    var copy = this.CreateCopy();
                    var cteDataSource = this.ConvertToCte(Guid.NewGuid(), copy);
                    // after above method call, this instance will be re-initialized
                    // and it will have the 'copy' as CTE Data Source in it and
                    // it will have CTE Reference as Initial Data Source
                    sqlExpression = this.ReplaceInnerQueryColumnAccessWithOuterQueryColumnAccess(sqlExpression, cteDataSource, copy.Projection);
                }
            }
            return sqlExpression;
        }

        private SqlDataSourceExpression ConvertToCte(Guid cteAlias, SqlQueryExpression cteSource)
        {
            var cteReference = new SqlDataSourceExpression(cteAlias, new SqlCteReferenceExpression(cteAlias));
            this.Initialize(cteReference);
            var actualDataSource = new SqlDataSourceExpression(cteAlias, cteSource, modelPath: ModelPath.Empty, tag: null, nodeType: SqlExpressionType.CteDataSource);
            this.AddCteDataSource(actualDataSource);
            this.LastSqlOperation = null;
            this.lastMethodBeforeSelect = null;

            return actualDataSource;
        }

        private SqlExpression ReplaceInnerQueryColumnAccessWithOuterQueryColumnAccess(SqlExpression sqlExpression, SqlDataSourceExpression actualDataSource, SqlExpression projection)
        {
            var innerQueryProjection = projection;
            SqlColumnExpression[] wrappedQueryProjections = null;
            if (innerQueryProjection is SqlColumnExpression sqlColumn)
                wrappedQueryProjections = new[] { sqlColumn };
            else if (innerQueryProjection is SqlCollectionExpression sqlCollection &&
                            sqlCollection.SqlExpressions.Any(y => y is SqlColumnExpression))
                wrappedQueryProjections = sqlCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
            if (wrappedQueryProjections != null)
            {
                var visitor = new SubQueryColumnAccessReplacementVisitor(wrappedQueryProjections, actualDataSource);
                sqlExpression = visitor.Visit(sqlExpression);
            }

            return sqlExpression;
        }

        class SubQueryColumnAccessReplacementVisitor : SqlExpressionVisitor
        {
            private readonly SqlColumnExpression[] wrappedQueryProjections;
            private readonly Dictionary<SqlExpression, int> expressionHash = new Dictionary<SqlExpression, int>();
            private readonly SqlDataSourceExpression newDataSource;
            private readonly SqlExpressionHashGenerator sqlExpressionHashGenerator = new SqlExpressionHashGenerator();

            public SubQueryColumnAccessReplacementVisitor(SqlColumnExpression[] wrappedQueryProjections, SqlDataSourceExpression newDataSource)
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
            }

            public override SqlExpression Visit(SqlExpression node)
            {
                var subQueryColumn = this.wrappedQueryProjections.Where(x => x.ColumnExpression == node ||
                                                                                this.expressionHash[x.ColumnExpression] == sqlExpressionHashGenerator.GenerateHash(node))
                                                                    .FirstOrDefault();
                if (subQueryColumn != null)
                {
                    // here we are creating the new column access expression that is using innerQueryDataSource
                    return new SqlDataSourceColumnExpression(newDataSource, subQueryColumn.ColumnAlias);
                }
                return base.Visit(node);
            }
        }

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
                this.ValidateOuterDataSourceUsageInCte(sqlDataSourceColumnExpression.DataSource.DataSourceAlias);
                return base.VisitSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
            }

            private void ValidateOuterDataSourceUsageInCte(Guid dataSourceAlias)
            {
                if (this.cteDataSource.Count > 0)
                    if (this.sourceQuery.AllDataSources.Any(x => x.DataSourceAlias == dataSourceAlias))
                        throw new InvalidOperationException($"Outer data source is being used in a CTE Query.");
            }
        }

        class CteDataSourceSearchVisitor : SqlExpressionVisitor
        {
            private readonly SqlQueryExpression sourceQuery;
            private readonly Stack<SqlDataSourceExpression> cteDataSource = new Stack<SqlDataSourceExpression>();
            public bool HasOuterDataSource { get; private set; }

            public static bool Find(SqlQueryExpression sourceQuery, SqlExpression sqlExpression)
            {
                var visitor = new CteDataSourceSearchVisitor(sourceQuery);
                visitor.Visit(sqlExpression);
                return visitor.HasOuterDataSource;
            }

            public CteDataSourceSearchVisitor(SqlQueryExpression sourceQuery)
            {
                this.sourceQuery = sourceQuery;
            }

            public override SqlExpression Visit(SqlExpression node)
            {
                if (this.HasOuterDataSource)
                    return node;
                return base.Visit(node);
            }

            protected internal override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
            {
                var doPop = false;
                if (sqlDataSourceExpression.NodeType == SqlExpressionType.CteDataSource)
                {
                    doPop = true;
                    this.cteDataSource.Push(sqlDataSourceExpression);
                }
                this.FindOuterDataSource(sqlDataSourceExpression.DataSourceAlias);
                if (this.HasOuterDataSource)
                    return sqlDataSourceExpression;
                var updatedNode = base.VisitSqlDataSourceExpression(sqlDataSourceExpression);
                if (doPop)
                    this.cteDataSource.Pop();
                return updatedNode;
            }

            protected internal override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
            {
                this.FindOuterDataSource(sqlDataSourceColumnExpression.DataSource.DataSourceAlias);
                if (this.HasOuterDataSource)
                    return sqlDataSourceColumnExpression;
                return base.VisitSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
            }

            private void FindOuterDataSource(Guid dataSourceAlias)
            {
                if (this.cteDataSource.Count > 0)
                    if (this.sourceQuery.AllDataSources.Any(x => x.DataSourceAlias == dataSourceAlias))
                        this.HasOuterDataSource = true;
                        //throw new InvalidOperationException($"Outer data source is being used in a CTE Query.");
            }
        }
    }
}
