using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// Represents the various operations that can be performed in a SQL query.
    /// </summary>
    public enum SqlQueryOperation
    {
        /// <summary>
        /// Represents a SELECT operation in a SQL query.
        /// </summary>
        Select,

        /// <summary>
        /// Represents a JOIN operation in a SQL query.
        /// </summary>
        Join,

        /// <summary>
        /// Represents a WHERE operation in a SQL query.
        /// </summary>
        Where,

        /// <summary>
        /// Represents a GROUP BY operation in a SQL query.
        /// </summary>
        GroupBy,

        /// <summary>
        /// Represents an ORDER BY operation in a SQL query.
        /// </summary>
        OrderBy,

        /// <summary>
        /// Represents a TOP operation in a SQL query.
        /// </summary>
        Top,

        /// <summary>
        /// Represents a DISTINCT operation in a SQL query.
        /// </summary>
        Distinct,

        /// <summary>
        /// Represents a ROW OFFSET operation in a SQL query.
        /// </summary>
        RowOffset,

        /// <summary>
        /// Represents a ROWS PER PAGE operation in a SQL query.
        /// </summary>
        RowsPerPage,

        /// <summary>
        /// Represents a UNION operation in a SQL query.
        /// </summary>
        Union,
    }

    /// <summary>
    ///     <para>
    ///         Represents a SQL query expression.
    ///     </para>
    /// </summary>
    public partial class SqlQueryExpression : SqlQuerySourceExpression
    {
        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Query;

        public virtual ISqlExpressionFactory SqlFactory { get; }

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
                return (this.InitialDataSource != null ? new[] { this.InitialDataSource } : Array.Empty<SqlDataSourceExpression>())
                                .Concat(this.joins.Select(x => x.JoinedSource)).ToArray();
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
        public IReadOnlyCollection<SqlDataSourceExpression> AllQuerySources => CombinedDataSources.Where(x => !(x.QuerySource is SqlCteReferenceExpression)).Concat(this.cteDataSources).Concat(this.subQueryDataSources).ToArray();

        public IReadOnlyCollection<SqlDataSourceExpression> AllDataSources => CombinedDataSources.Concat(this.cteDataSources).Concat(this.subQueryDataSources).ToArray();

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
        private readonly List<SqlDataSourceExpression> subQueryDataSources = new List<SqlDataSourceExpression>();
        /// <summary>
        ///     <para>
        ///         Gets the list of other data sources that were created and added to sql query.
        ///     </para>
        ///     <para>
        ///         Usually GroupJoin data sources are added to this collection.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<SqlDataSourceExpression> SubQueryDataSources => this.subQueryDataSources;
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
        public bool IsMultiDataSourceQuery => this.CombinedDataSources.Where(x => x .NodeType == SqlExpressionType.FromSource).Any();
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
        /// <param name="sqlFactory">ISqlExpressionFactory instance</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="dataSource"/> is <c>null</c>.</exception>
        public SqlQueryExpression(SqlDataSourceExpression dataSource, ISqlExpressionFactory sqlFactory)
        {
            if (dataSource is null)
                throw new ArgumentNullException(nameof(dataSource));
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));
            this.AddDataSources(dataSource);
        }

        /// <summary>
        ///     <para>
        ///         Creates a new instance of the <see cref="SqlQueryExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="dataSources">List of data sources to be used in the query.</param>
        /// <param name="sqlFactory">ISqlExpressionFactory instance</param>
        public SqlQueryExpression(IEnumerable<SqlDataSourceExpression> dataSources, ISqlExpressionFactory sqlFactory)
        {
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));
            this.AddDataSources(dataSources?.ToArray());
        }

        public SqlQueryExpression(Guid cteAlias, SqlQueryExpression cteSource, ISqlExpressionFactory sqlFactory)
        {
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));
            this.ConvertToCte(cteAlias, cteSource);
        }

        public SqlQueryExpression(SqlExpression select, ISqlExpressionFactory sqlFactory)
        {
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));
            if(!(select is SqlColumnExpression || ((select as SqlCollectionExpression)?.SqlExpressions.All(x => x is SqlColumnExpression) ?? false)))
                throw new ArgumentException("Projection must be of type SqlColumnExpression.", nameof(select));
            this.Projection = select ?? throw new ArgumentNullException(nameof(select));
            this.LastSqlOperation = SqlQueryOperation.Select;
        }

        public SqlQueryExpression(ISqlExpressionFactory sqlFactory)
        {
            this.SqlFactory = sqlFactory ?? throw new ArgumentNullException(nameof(sqlFactory));
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
            if (usualDataSourceOrCteDataSource.QuerySource is SqlCteReferenceExpression cteRef)
            {
                return this.cteDataSources.Where(x => x.DataSourceAlias == cteRef.CteAlias).FirstOrDefault();
            }
            return usualDataSourceOrCteDataSource;
        }

        public void AddSubQueryDataSource(SqlDataSourceExpression sqlDataSourceExpression)
        {
            sqlDataSourceExpression.AttachToParentSqlQuery(this);
            this.subQueryDataSources.Add(sqlDataSourceExpression);
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
                        this.CombinedDataSources[0].QuerySource is SqlTableExpression &&
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
            this.Projection = this.CreateSqlLiteral(1);
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
        public SqlJoinExpression AddCrossJoin(SqlDataSourceExpression joinedDataSource)
        {
            return this.AddCrossJoin(joinedDataSource, checkWrap: true);
        }

        private SqlJoinExpression AddCrossJoin(SqlDataSourceExpression joinedDataSource, bool checkWrap)
        {
            if (checkWrap)
                joinedDataSource = this.WrapIfRequired(joinedDataSource, SqlQueryOperation.Join) as SqlDataSourceExpression
                                    ?? throw new InvalidOperationException("Joined data source must be of type SqlDataSourceExpression.");

            return this.AddDataSources(joinedDataSource).FirstOrDefault();
        }

        /// <summary>
        ///     <para>
        ///         Adds a child data source as join and also maps it with the parent data source within this query.
        ///     </para>
        /// </summary>
        /// <param name="navigationParent">Parent SQL Expression which can be this query instance or a child data source within this query.</param>
        /// <param name="childDataSourceExpression">A new data source to be added to the query.</param>
        /// <param name="navigationName">Navigation name to identify this child data source.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public SqlJoinExpression AddNavigationJoin(SqlExpression navigationParent, SqlDataSourceExpression childDataSourceExpression, string navigationName)
        {
            if (navigationParent is null)
                throw new ArgumentNullException(nameof(navigationParent));
            if (childDataSourceExpression is null)
                throw new ArgumentNullException(nameof(childDataSourceExpression));
            if (childDataSourceExpression.ParentSqlQuery != null)
                throw new InvalidOperationException($"Argument '{nameof(childDataSourceExpression)}' is already a part of another SQL query.");
            if (string.IsNullOrEmpty(navigationName))
                throw new ArgumentNullException(nameof(navigationName));
            if (navigationParent != this)
            {
                // then it must be the child data source within this query
                if (!this.DataSources.Any(x => x == navigationParent))
                    throw new InvalidOperationException($"Argument '{nameof(navigationParent)}' is not a part of Data Source within this SQL query.");
            }

            var sqlJoin = this.AddCrossJoin(childDataSourceExpression, checkWrap: this.LastSqlOperation != SqlQueryOperation.Where);
            sqlJoin.SetNavigationInfo(navigationParent, navigationName);

            return sqlJoin;
        }

        private SqlJoinExpression[] AddDataSources(params SqlDataSourceExpression[] dataSources)
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
            var crossJoins = dataSourcesToAdd.Select(x => this.CreateCrossJoin(x)).ToArray();

            // by default any data source that is added to the query will be added
            // as Cross Join
            this.joins.AddRange(crossJoins);

            return crossJoins;
        }

        protected virtual SqlJoinExpression CreateCrossJoin(SqlDataSourceExpression joinedSource)
        {
            return this.CreateJoin(SqlJoinType.Cross, joinedSource, joinPredicate: null);
        }

        protected virtual SqlJoinExpression CreateJoin(SqlJoinType joinType, SqlDataSourceExpression joinedDataSource, SqlExpression joinPredicate)
        {
            return this.SqlFactory.CreateJoin(joinType, joinedDataSource, joinPredicate);
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
            if (selector is ISqlReferenceExpression refExpr1)
            {
                var columns = this.GetColumnsFromDataSourceReference(refExpr1, ModelPath.Empty);
                projection = this.CreateSqlCollection(columns);
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
                    // TODO: check if we can use only SqlDataSourceReferenceExpression
                    if (colExpr.ColumnExpression is ISqlReferenceExpression refExpression)
                    {
                        var dsColumns = this.GetColumnsFromDataSourceReference(refExpression, colExpr.ModelPath);
                        columns.AddRange(dsColumns);
                    }
                    else
                        columns.Add(colExpr);
                }
                projection = this.CreateSqlCollection(columns);
            }
            else
            {
                projection = this.CreateSqlScalarColumn(selector, "Col1", ModelPath.Empty);
            }
            this.ApplyProjectionInternal(projection);
        }

        protected virtual SqlCollectionExpression CreateSqlCollection(IEnumerable<SqlColumnExpression> columns)
        {
            return this.SqlFactory.CreateCollection(columns);
        }

        protected virtual SqlColumnExpression CreateSqlScalarColumn(SqlExpression columnExpression, string columnAlias, ModelPath modelPath)
        {
            return this.SqlFactory.CreateScalarColumn(columnExpression, columnAlias, modelPath);
        }

        private void ApplyProjectionInternal(SqlExpression projection)
        {
            if (!(projection is SqlColumnExpression colExpr && colExpr.NodeType == SqlExpressionType.ScalarColumn))
            {
                var givenProjections = projection.GetProjections().ToList();
                var newProjections = new List<SqlColumnExpression>();
                for (var i = 0; i < givenProjections.Count; i++)
                {
                    var givenProjection = givenProjections[i];
                    this.ConvertSubQueryProjectionToOuterApply(givenProjection, newProjections);
                }
                projection = this.CreateSqlCollection(newProjections);
            }
            projection = this.FixDuplicateColumns(projection);
            this.Projection = projection;

            //this.NavigationDataSourceMap.Clear();

            this.ClearNavigationMap();
        }

        private void ClearNavigationMap()
        {
            for (int i = 0; i < this.joins.Count; i++)
            {
                var join = this.joins[i];
                if (join.IsNavigationJoin)
                {
                    join.ClearNavigationalInfo();
                }
            }
        }

        private void ConvertSubQueryProjectionToOuterApply(SqlColumnExpression sqlColumnExpression, List<SqlColumnExpression> projections)
        {
            // sqlColumnExpression might be like this
            //      (select top 1 a_1.Column from Table as a_1 where outerTable.Column = a_1.Column) as g
            // so we are picking ColumnExpression which will be a sub-query and if that sub-query is *NOT* single
            // value query then we'll make it outer apply to this query
            if (sqlColumnExpression.ColumnExpression is SqlQueryExpression subQuery && !subQuery.IsSingleValueQuery())
            {
                subQuery.ApplyAutoProjection();
                var subQueryAsDataSourceInThisQuery = this.CreateDataSourceFromQuery(subQuery);
                var joinExpression = this.CreateJoin(SqlJoinType.OuterApply, subQueryAsDataSourceInThisQuery, joinPredicate: null);
                this.joins.Add(joinExpression);

                var subQueryProjections = subQuery.Projection.GetProjections();
                for (var i = 0; i < subQueryProjections.Length; i++)
                {
                    var subQueryProjection = subQueryProjections[i];
                    var newSubQueryColumnSelection = this.CreateSqlDataSourceColumn(subQueryAsDataSourceInThisQuery, subQueryProjection.ColumnAlias);
                    var newProjection = this.CreateSqlSubQueryColumn(newSubQueryColumnSelection, subQueryProjection.ColumnAlias, sqlColumnExpression.ModelPath.Append(subQueryProjection.ColumnAlias)/*, subQuery*/);
                    projections.Add(newProjection);
                }
            }
            else
            {
                projections.Add(sqlColumnExpression);
            }
        }

        protected virtual SqlColumnExpression CreateSqlSubQueryColumn(SqlDataSourceColumnExpression columnExpression, string columnAlias, ModelPath modelPath/*, SqlQueryExpression subQuery*/)
        {
            return this.SqlFactory.CreateSubQueryColumn(columnExpression, columnAlias, modelPath);
        }

        protected virtual SqlDataSourceExpression CreateDataSourceFromQuery(SqlQueryExpression subQuery)
        {
            return this.SqlFactory.CreateDataSourceForQuerySource(subQuery);
        }

        protected virtual SqlDataSourceColumnExpression CreateSqlDataSourceColumn(SqlDataSourceExpression dataSource, string columnAlias)
        {
            return this.SqlFactory.CreateDataSourceColumn(dataSource, columnAlias);
        }

        public bool IsSingleValueQuery()
        {
            var sqlQuery = this;
            if (sqlQuery.Top != null &&
                    ((sqlQuery.Top is SqlParameterExpression sqlParameter && (sqlParameter.Value?.Equals(1) ?? false))
                        ||
                    (sqlQuery.Top is SqlLiteralExpression sqlLiteral && (sqlLiteral.LiteralValue?.Equals(1) ?? false))))
            {
                if ((sqlQuery.Projection is SqlColumnExpression)
                    ||
                    (sqlQuery.Projection is SqlCollectionExpression collection &&
                    collection.SqlExpressions.Count() == 1))
                    return true;
            }
            if (sqlQuery.Projection is SqlColumnExpression colExpr && colExpr.NodeType == SqlExpressionType.ScalarColumn)
                return true;
            return false;
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
                        colToAdd = this.ChangeSqlColumnExpressionAlias(currentCol, columnAlias);
                        projectionChanged = true;
                    }
                    colDictionary.Add(columnAlias, colToAdd);
                }
                if (projectionChanged)
                    projection = this.CreateSqlCollection(colDictionary.Values.ToArray());
            }

            return projection;
        }

        private SqlColumnExpression ChangeSqlColumnExpressionAlias(SqlColumnExpression sqlColumnExpression, string columnAlias)
        {
            return this.SqlFactory.ChangeColumnAlias(sqlColumnExpression, columnAlias);
        }

        protected virtual SqlColumnExpression CreateSqlColumn(SqlExpression sqlExpression, string columnAlias, ModelPath modelPath)
        {
            return this.SqlFactory.CreateColumn(sqlExpression, columnAlias, modelPath);
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

        public SqlJoinExpression GetJoinByDataSource(SqlDataSourceExpression ds)
        {
            return this.joins.Where(x => x.JoinedSource == ds).FirstOrDefault();
        }

        public SqlExpression GetScalarColumnExpression()
        {
            if (this.Projection.TryGetScalarColumn(out var scalarCol))
            {
                return scalarCol.ColumnExpression;
            }
            else if (this.CombinedDataSources.FirstOrDefault()?.QuerySource is SqlQueryExpression subQuery && 
                        subQuery.Projection.TryGetScalarColumn(out var subQueryScalarCol))
            {
                return this.CreateSqlDataSourceColumn(this.CombinedDataSources[0], subQueryScalarCol.ColumnAlias);
            }
            // TODO: check if we need to cater the case of DefaultDataSource
            return null;
        }

        /// <summary>
        ///     <para>
        ///         Wraps this query in a sub-query.
        ///     </para>
        /// </summary>
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
        public virtual void WrapInSubQuery()
        {
            ApplyAutoProjectionInternal(applyAll: true);

            var copy = CreateCopy();

            // here a new alias will be generated for sub-query
            var newDataSource = this.CreateDataSourceFromQuery(subQuery: copy);
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
            //this.NavigationDataSourceMap.Clear();
            //this.autoJoins.Clear();
            this.LastSqlOperation = null;
        }

        /// <summary>
        ///     <para>
        ///         Creates of copy of <paramref name="source"/>.
        ///     </para>
        /// </summary>
        /// <param name="source">A SQL query expression to be copied.</param>
        /// <returns>A new instance of <see cref="SqlQueryExpression"/> with copied properties.</returns>
        protected static SqlQueryExpression CreateCopy(SqlQueryExpression source, ISqlExpressionFactory sqlFactory)
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

            var destination = sqlFactory.CreateEmptySqlQuery();            
            Copy(source, destination);

            return destination;
        }

        protected static void Copy(SqlQueryExpression source, SqlQueryExpression copy)
        {
            var sqlFactory = source.SqlFactory;

            if (source.InitialDataSource != null)
                copy.InitialDataSource = sqlFactory.CreateDataSourceCopy(source.InitialDataSource);
            copy.Projection = source.Projection;
            copy.whereClause.AddRange(source.whereClause);
            copy.havingClauseList.AddRange(source.havingClauseList);

            var joinsCopy = source.joins.Select(x => sqlFactory.CreateJoin(x.JoinType, sqlFactory.CreateDataSourceCopy(x.JoinedSource), x.JoinCondition));
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
        }

        /// <summary>
        ///     <para>
        ///         Tries to get the child data source by the given <paramref name="navigationName"/> and <paramref name="navigationParent"/>.
        ///     </para>
        /// </summary>
        /// <param name="navigationParent">Parent Data Source of the navigation. This will be a <see cref="SqlDataSourceExpression"/> within this query instance.</param>
        /// <param name="navigationName">Navigation name.</param>
        /// <param name="childDataSource">Child Data Source to which we are navigating to. This will be a <see cref="SqlDataSourceExpression"/> within this query instance.</param>
        /// <returns><c>true</c> if the child data source is found; otherwise, <c>false</c>.</returns>
        public bool TryGetNavigationDataSource(SqlExpression navigationParent, string navigationName, out SqlDataSourceExpression childDataSource)
        {
            var join = this.joins.Where(x => x.NavigationParent == navigationParent && x.NavigationName == navigationName).FirstOrDefault();
            if (join != null)
            {
                childDataSource = join.JoinedSource;
                return true;
            }
            childDataSource = null;
            return false;
        }

        /// <summary>
        ///     <para>
        ///         Creates a copy of this query.
        ///     </para>
        /// </summary>
        /// <returns>A new instance of <see cref="SqlQueryExpression"/> with copied properties.</returns>
        public virtual SqlQueryExpression CreateCopy()
        {
            var copy = CreateCopy(this, this.SqlFactory);
            return copy;
        }

        protected virtual SqlQueryExpression CreateSqlQueryFromDataSource(SqlDataSourceExpression dataSource)
        {
            return this.SqlFactory.CreateQueryFromDataSource(dataSource);
        }

        protected virtual SqlDataSourceExpression CreateSqlDataSourceCopy(SqlDataSourceExpression dataSource)
        {
            return this.SqlFactory.CreateDataSourceCopy(dataSource);
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
                if (this.GroupBy != null)
                {
                    var groupBy = this.GroupBy;
                    if (!(this.GroupBy is SqlCollectionExpression || this.GroupBy is SqlColumnExpression))
                    {
                        groupBy = this.CreateSqlColumn(groupBy, "Col1", ModelPath.Empty);
                    }
                    this.ApplyProjectionInternal(groupBy);
                }
                else
                {
                    List<SqlColumnExpression> autoProjection = this.GetAutoProjection(applyAll);
                    var projectionExpression = this.CreateSqlCollection(autoProjection);
                    this.ApplyProjectionInternal(projectionExpression);
                }
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
                if (ds.QuerySource is SqlCteReferenceExpression cteRef)
                {
                    ds = this.cteDataSources.Where(x => x.DataSourceAlias == cteRef.CteAlias).FirstOrDefault()
                                ??
                                throw new InvalidOperationException($"CTE data source not found for alias '{cteRef.CteAlias}'.");
                }
                if (ds.QuerySource is SqlTableExpression table)
                {
                    foreach (var tableColumn in table.TableColumns)
                    {
                        var columnAlias = tableColumn.ModelPropertyName;
                        var dataSourceColExpression = this.CreateSqlDataSourceColumn(ds, tableColumn.DatabaseColumnName);
                        var sqlColExpression = this.CreateSqlColumn(dataSourceColExpression, tableColumn.ModelPropertyName, ds.ModelPath.Append(tableColumn.ModelPropertyName));
                        autoProjection.Add(sqlColExpression);
                    }
                }
                else if (ds.QuerySource is SqlQueryExpression subQuery)
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
                                var dataSourceColExpression = this.CreateSqlDataSourceColumn(ds, sqlColumn.ColumnAlias);
                                var newSqlColumnExpression = this.CreateSqlColumn(dataSourceColExpression, sqlColumn.ColumnAlias, ds.ModelPath.Append(sqlColumn.ColumnAlias));
                                autoProjection.Add(newSqlColumnExpression);
                            }
                            else
                                throw new InvalidOperationException($"Unexpected expression type in sub-query projection: {sqlExpression.NodeType}");
                        }
                    }
                    else if (subQuery.Projection.TryGetScalarColumn(out var scalarCol))
                    {
                        var columnAlias = scalarCol.ColumnAlias;
                        var dataSourceColExpression = this.CreateSqlDataSourceColumn(ds, columnAlias);
                        var newSqlColumnExpression = this.CreateSqlColumn(dataSourceColExpression, columnAlias, ds.ModelPath.Append(scalarCol.ColumnAlias));
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
            return join?.IsNavigationJoin == true;
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
                return $"TableOnly-Query: {(this.InitialDataSource.QuerySource as SqlTableExpression).TableName}";
            }
            else
            {
                var dataSourcesAliases = string.Join(", ", this.AllQuerySources.Select(x => DebugAliasGenerator.GetAlias(x)));
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
            groupByExpression = this.WrapIfRequired(groupByExpression, SqlQueryOperation.GroupBy);
            if (this.GroupBy != null)
                throw new InvalidOperationException("GroupBy already set.");
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
                        orderByExpression = this.CreateSqlOrderBy(this.CreateSqlAlias(matchedExpression.ColumnAlias), orderByExpression.Ascending);
                }
                else if (this.Projection.TryGetScalarColumn(out var scalarCol) && scalarCol.ColumnExpression == orderByExpression.Expression)
                {
                    orderByExpression = this.CreateSqlOrderBy(this.CreateSqlAlias(scalarCol.ColumnAlias), orderByExpression.Ascending);
                }
            }
            this.orderBy.Add(orderByExpression);
        }

        protected virtual SqlOrderByExpression CreateSqlOrderBy(SqlExpression sqlExpression, bool ascending)
        {
            return this.SqlFactory.CreateOrderBy(sqlExpression, ascending);
        }

        protected virtual SqlAliasExpression CreateSqlAlias(string columnAlias)
        {
            return this.SqlFactory.CreateAlias(columnAlias);
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
                this.orderBy.Add(this.CreateSqlOrderBy(this.CreateSqlLiteral(1), true));
        }

        protected virtual SqlLiteralExpression CreateSqlLiteral(object value)
        {
            return this.SqlFactory.CreateLiteral(value);
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
                this.orderBy.Add(this.CreateSqlOrderBy(this.CreateSqlLiteral(1), ascending: true));
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
        /// <param name="cteDataSources">New CTE data sources to be applied to the query.</param>
        /// <param name="havingClause">New having clause to be applied to the query.</param>
        /// <returns>A new instance of <see cref="SqlQueryExpression"/> with updated properties.</returns>
        public SqlQueryExpression Update(SqlDataSourceExpression initialDataSource, IEnumerable<SqlJoinExpression> joins, IEnumerable<FilterPredicate> whereClause, SqlExpression groupBy, SqlExpression projection, IEnumerable<SqlOrderByExpression> orderByClause, SqlExpression top, IEnumerable<SqlDataSourceExpression> cteDataSources, IEnumerable<FilterPredicate> havingClause, IEnumerable<SqlUnionExpression> unions)
        {
            if (this.InitialDataSource == initialDataSource && this.joins?.SequenceEqual(joins) == true && this.whereClause?.SequenceEqual(whereClause) == true && this.GroupBy == groupBy && this.Projection == projection && this.orderBy?.SequenceEqual(orderByClause) == true && this.Top == top && this.cteDataSources?.SequenceEqual(cteDataSources) == true && this.havingClauseList.SequenceEqual(havingClause) && this.unions?.SequenceEqual(unions) == true)
                return this;

            var initialDataSourceCopy = this.SqlFactory.CreateDataSourceCopy(initialDataSource);
            var sqlQuery = this.SqlFactory.CreateQueryFromDataSource(initialDataSourceCopy);
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
                }
                else
                {
                    var newJoin = this.CreateJoin(join.JoinType, this.CreateSqlDataSourceCopy(join.JoinedSource), join.JoinCondition);
                    newJoin.JoinedSource.AttachToParentSqlQuery(sqlQuery);
                    sqlQuery.joins.Add(newJoin);
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
                    dsToAdd = this.CreateSqlDataSourceCopy(cteDataSource);
                sqlQuery.cteDataSources.Add(dsToAdd);
            }

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

            this.unions.Add(this.CreateSqlUnion(query, unionAll));
        }

        protected virtual SqlUnionExpression CreateSqlUnion(SqlQueryExpression query, bool unionAll)
        {
            if (unionAll)
                return this.SqlFactory.CreateUnionAll(query);
            else
                return this.SqlFactory.CreateUnion(query);
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


        private IEnumerable<SqlColumnExpression> GetColumnsFromDataSourceReference(ISqlReferenceExpression refExpression, ModelPath modelPath)
        {
            var dataSource = refExpression.Reference;
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
            var tableOrSubQuery = ds.QuerySource;
            List<SqlColumnExpression> columns = new List<SqlColumnExpression>();
            if (tableOrSubQuery is SqlTableExpression table)
            {
                var tableColumns = table.TableColumns.Select(x => this.CreateSqlColumn(this.CreateSqlDataSourceColumn(ds, x.DatabaseColumnName), x.DatabaseColumnName, modelPath.Append(x.ModelPropertyName)));
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
                        subQueryColumns.Add(this.CreateSqlColumn(this.CreateSqlDataSourceColumn(ds, colExpr.ColumnAlias), colExpr.ColumnAlias, colModelPath));
                    }
                    else
                        throw new InvalidOperationException($"sqlExpr is not SqlColumnExpression");
                }
            }
            else if (subQueryProjection is SqlColumnExpression colExpr)
            {
                var colModelPath = modelPath.Append(colExpr.ModelPath);
                subQueryColumns.Add(this.CreateSqlColumn(this.CreateSqlDataSourceColumn(ds, colExpr.ColumnAlias), colExpr.ColumnAlias, colModelPath));
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

        protected virtual SqlExpression WrapIfRequired(SqlExpression expression, SqlQueryOperation newOperation)
        {
            if (expression != null)
                expression = this.CheckAndApplyCteExpression(expression);

            var performWrap = this.IsWrapRequired(newOperation);

            if (performWrap)
            {
                this.WrapInSubQuery();
            }

            var innerQueryDataSource = this.DataSources.FirstOrDefault();
            if (expression != null && 
                innerQueryDataSource != null && 
                innerQueryDataSource.QuerySource is SqlQueryExpression innerQueryQuery &&
                innerQueryQuery.InitialDataSource != null)
            {
                var innerQueryProjection = innerQueryQuery.Projection;
                expression = this.ReplaceInnerQueryColumnAccessWithOuterQueryColumnAccess(expression, innerQueryDataSource, innerQueryProjection);
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

        protected virtual bool IsWrapRequired(SqlQueryOperation newOperation)
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
                    this.ApplyAutoProjectionInternal(applyAll: true);

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
            var cteReference = this.CreateSqlDataSourceForCteReference(cteAlias, this.CreateSqlCteReference(cteAlias));
            this.Initialize(cteReference);
            var actualDataSource = this.CreateSqlDataSourceForCte(cteAlias, cteSource);
            this.AddCteDataSource(actualDataSource);
            this.LastSqlOperation = null;
            this.lastMethodBeforeSelect = null;

            return actualDataSource;
        }

        protected virtual SqlDataSourceExpression CreateSqlDataSourceForCte(Guid cteAlias, SqlQueryExpression cteSource)
        {
            return this.SqlFactory.CreateDataSourceForCteQuery(cteAlias, cteSource);
        }

        protected virtual SqlDataSourceExpression CreateSqlDataSourceForCteReference(Guid cteAlias, SqlCteReferenceExpression sqlCteReferenceExpression)
        {
            return this.SqlFactory.CreateDataSourceForCteReference(cteAlias, sqlCteReferenceExpression);
        }

        protected virtual SqlCteReferenceExpression CreateSqlCteReference(Guid cteAlias)
        {
            return this.SqlFactory.CreateCteReference(cteAlias);
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
                var visitor = new SubQueryColumnAccessReplacementVisitor(wrappedQueryProjections, actualDataSource, this.CreateSqlDataSourceColumn);
                sqlExpression = visitor.Visit(sqlExpression);
            }

            return sqlExpression;
        }
    }
}
