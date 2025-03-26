using Atis.LinqToSql.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL data source expression.
    ///     </para>
    /// </summary>
    public class SqlDataSourceExpression : SqlExpression
    {
        private static readonly ISet<SqlExpressionType> _allowedTypes = new HashSet<SqlExpressionType>
            {
                SqlExpressionType.DataSource,
                SqlExpressionType.CteDataSource,
                SqlExpressionType.SubQueryDataSource,      // this data source will be added because of GroupJoin
                SqlExpressionType.FromSource,
            };
        private static SqlExpressionType ValidateNodeType(SqlExpressionType nodeType)
            => _allowedTypes.Contains(nodeType)
                ? nodeType
                : throw new InvalidOperationException($"SqlExpressionType '{nodeType}' is not a valid SqlDataSourceExpression.");

        /// <inheritdoc />
        public override SqlExpressionType NodeType { get; }

        public SqlDataSourceExpression(SqlQuerySourceExpression dataSource)
            : this(dataSource, modelPath: ModelPath.Empty, tag: null)
        {
        }

        public SqlDataSourceExpression(Guid dataSourceAlias, SqlQuerySourceExpression dataSource)
            : this(dataSourceAlias: dataSourceAlias, dataSource: dataSource, modelPath: ModelPath.Empty, tag: null)
        {
        }

        public SqlDataSourceExpression(SqlQuerySourceExpression dataSource, ModelPath modelPath, string tag)
            : this(Guid.NewGuid(), dataSource, modelPath, tag)
        {
        }

        public SqlDataSourceExpression(Guid dataSourceAlias, SqlQuerySourceExpression dataSource, ModelPath modelPath, string tag, SqlExpressionType nodeType = SqlExpressionType.DataSource)
        {
            this.QuerySource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            this.DataSourceAlias = dataSourceAlias;// ?? throw new ArgumentNullException(nameof(dataSourceAlias));
            this.ModelPath = modelPath;
            this.Tag = tag;
            ValidateNodeType(nodeType);
            this.NodeType = nodeType;
        }

        public SqlDataSourceExpression(SqlDataSourceExpression copyFrom)
        {
            this.QuerySource = copyFrom.QuerySource;
            this.DataSourceAlias = copyFrom.DataSourceAlias;
            this.ModelPath = copyFrom.ModelPath;
            this.Tag = copyFrom.Tag;
            this.NodeType = copyFrom.NodeType;
        }

        /// <summary>
        ///     <para>
        ///         Gets the data source expression.
        ///     </para>
        /// </summary>
        public SqlQuerySourceExpression QuerySource { get; }
        /// <summary>
        ///     <para>
        ///         Gets the parent SQL query expression.
        ///     </para>
        /// </summary>
        public SqlQueryExpression ParentSqlQuery { get; private set; }
        /// <summary>
        ///     <para>
        ///         Gets the alias of the data source.
        ///     </para>
        /// </summary>
        public Guid DataSourceAlias { get; }
        /// <summary>
        ///     <para>
        ///         Gets the model path associated with the data source.
        ///     </para>
        /// </summary>
        public ModelPath ModelPath { get; private set; }

        /// <summary>
        ///     <para>
        ///         Gets the tag associated with the data source.
        ///     </para>
        /// </summary>
        public string Tag { get; }

        /// <summary>
        ///     <para>
        ///         Attaches this data source expression to a parent SQL query.
        ///     </para>
        /// </summary>
        /// <param name="parentSqlQuery">The parent SQL query to attach to.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void AttachToParentSqlQuery(SqlQueryExpression parentSqlQuery)
        {
            if (this.ParentSqlQuery != null)
                throw new InvalidOperationException("ParentSqlQuery is already set.");
            this.ParentSqlQuery = parentSqlQuery ?? throw new ArgumentNullException(nameof(parentSqlQuery));
        }

        public SqlDataSourceExpression Update(SqlQuerySourceExpression dataSource)
        {
            if (dataSource == this.QuerySource)
                return this;
            return new SqlDataSourceExpression(this.DataSourceAlias, dataSource, this.ModelPath, this.Tag, nodeType: this.NodeType);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL data source expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlDataSourceExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL data source expression.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL data source expression.</returns>
        public override string ToString()
        {
            return $"dataSource-{this.QuerySource.NodeType}: {DebugAliasGenerator.GetAlias(this)}";
        }

        /// <summary>
        ///     <para>
        ///         Replaces the last part of the Model Path with the given <paramref name="modelPathPrefix"/>.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Used when the Data Source Model Mapping is changed. Usually this happens when the original
        ///         Data Source Member is replaced with a new one.
        ///     </para>
        ///     <para>
        ///         For example,
        ///     </para>
        ///     <code>
        ///         var q1 = From(() => new { t1 = Table&lt;Table1&gt;(), t2 = Table&lt;Table2&gt;() });
        ///     </code>
        ///     <para>
        ///         In above example, we have 2 data sources in the query. First data source is mapped with
        ///         Member 't1' and second data source is mapped with Member 't2'. The Model Path of Data Sources
        ///         would be t1 and t2 respectively. Let say, user did a join and changed this mapping.
        ///     </para>
        ///     <code>
        ///         var q2 = q1.LeftJoin(DataSet&lt;Table3&gt;(), 
        ///                                 (oldType, joinedType) => new { table1 = oldType.t1, table2 = oldType.t2, table3 = joinedType },
        ///                                 (newShape) => newShape.table3.FK == newShape.table1.PK);
        ///     </code>
        ///     <para>
        ///         Now as we can see, to access the fields of Table1, we will be using 'table1' instead of 't1'. That's where
        ///         this method is used to replace the last part of Data Source's Model Path with a new one.
        ///     </para>
        /// </remarks>
        /// <param name="modelPathPrefix">New prefix to be added in the Model Path.</param>
        public void ReplaceModelPathPrefix(string modelPathPrefix)
        {
            this.AddOrReplaceModelPathPrefix(modelPathPrefix, replace: true);
        }

        /// <summary>
        ///     <para>
        ///         Adds a new prefix to the Model Path.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Similar to <see cref="ReplaceModelPathPrefix(string)"/> but instead of replacing the last part of the
        ///         Model Path, it adds a new prefix in the start of the Model Path.
        ///     </para>
        ///     <para>
        ///         For example, (we are using same example from <see cref="ReplaceModelPathPrefix(string)"/>)
        ///     </para>
        ///     <code>
        ///         var q2 = q1.LeftJoin(DataSet&lt;Table3&gt;(),
        ///                                 (oldType, joinedType) => new { oldType, table3 = joinedType },
        ///                                 (newShape) => newShape.table3.FK == newShape.oldType.t1.PK);
        ///     </code>
        ///     <para>
        ///         Now, to access the fields of Table1, we will be using 'oldType.t1' instead of 't1'. This tells us that
        ///         there is a new prefix 'oldType' added in the start of the Model Path.
        ///     </para>
        /// </remarks>
        /// <param name="modelPathPrefix">New prefix to be added in start of Model Path.</param>
        public void AddModelPathPrefix(string modelPathPrefix)
        {
            this.AddOrReplaceModelPathPrefix(modelPathPrefix, replace: false);
        }

        private void AddOrReplaceModelPathPrefix(string modelPathPrefix, bool replace)
        {
            if (replace)
                if (this.ModelPath.IsEmpty)
                    throw new InvalidOperationException($"replace == true while this.ModelPath is null");
            this.ModelPath = replace ? this.ModelPath.ReplaceLastPathEntry(modelPathPrefix) : new ModelPath(modelPathPrefix).Append(this.ModelPath);
        }
        
        /// <summary>
        ///     <para>
        ///         Gets the join type of this data source expression.
        ///     </para>
        /// </summary>
        /// <returns>The join type if available; otherwise, null.</returns>
        public SqlJoinType? GetJoinType()
        {
            return this.ParentSqlQuery?.GetJoinType(this);
        }
    }
}
