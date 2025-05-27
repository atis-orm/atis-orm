using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public partial class SqlDerivedTableExpression : SqlSubQuerySourceExpression
    {
        private static readonly ISet<SqlExpressionType> _allowedTypes = new HashSet<SqlExpressionType>
            {
                SqlExpressionType.DerivedTable,
                SqlExpressionType.UnwrappableDerivedTable,
                SqlExpressionType.DataManipulationDerivedTable,
            };
        private static SqlExpressionType ValidateNodeType(SqlExpressionType nodeType)
            => _allowedTypes.Contains(nodeType)
                ? nodeType
                : throw new InvalidOperationException($"SqlExpressionType '{nodeType}' is not a valid Derived Table Type.");

        public SqlDerivedTableExpression(IReadOnlyList<SqlAliasedCteSourceExpression> cteDataSources, SqlAliasedFromSourceExpression fromSource, IReadOnlyList<SqlAliasedJoinSourceExpression> joinedDataSources, SqlFilterClauseExpression whereClause, IReadOnlyList<SqlExpression> groupByClause, SqlFilterClauseExpression havingClause, SqlOrderByClauseExpression orderByClause, SqlSelectListExpression selectColumnCollection, bool isDistinct, int? top, int? rowOffset, int? rowsPerPage, bool autoProjection, string tag, SqlExpression queryShape, SqlExpressionType nodeType)
        {
            if (fromSource is null)
                throw new ArgumentNullException(nameof(fromSource));
            if (nodeType != SqlExpressionType.DataManipulationDerivedTable)
                if (!(selectColumnCollection?.SelectColumns?.Count > 0))
                    throw new ArgumentNullException(nameof(selectColumnCollection));

            this.NodeType = ValidateNodeType(nodeType);

            this.CteDataSources = cteDataSources ?? Array.Empty<SqlAliasedCteSourceExpression>();
            this.FromSource = fromSource;
            this.Joins = joinedDataSources ?? Array.Empty<SqlAliasedJoinSourceExpression>();
            this.WhereClause = whereClause;
            this.GroupByClause = groupByClause ?? Array.Empty<SqlExpression>();
            this.HavingClause = havingClause;
            this.OrderByClause = orderByClause;
            this.SelectColumnCollection = selectColumnCollection;
            this.IsDistinct = isDistinct;
            this.Top = top;
            this.RowOffset = rowOffset;
            this.RowsPerPage = rowsPerPage;
            this.AutoProjection = autoProjection;
            this.Tag = tag;
            this.QueryShape = queryShape ?? throw new ArgumentNullException(nameof(queryShape));

            this.IsCte = this.CteDataSources.Count > 0;

            this.IsTableOnly = this.AutoProjection &&
                                (this.FromSource.QuerySource is SqlTableExpression ||
                                    this.FromSource.QuerySource is SqlCteReferenceExpression) &&
                                this.Joins.Count == 0 &&
                                this.CteDataSources.Count == 0 &&
                                !(this.HavingClause?.FilterConditions.Length > 0) &&
                                this.GroupByClause.Count == 0 &&
                                !(this.OrderByClause?.OrderByColumns.Count > 0) &&
                                this.Top == null &&
                                this.IsDistinct == false &&
                                this.RowOffset == null &&
                                this.RowsPerPage == null &&
                               !(this.WhereClause?.FilterConditions?.Length > 0);

            this.AllDataSources = ((IEnumerable<SqlAliasedDataSourceExpression>)new[] { this.FromSource })
                                        .Concat(this.Joins)
                                        .ToArray();
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType { get; }
        public bool IsCte { get; }
        public bool IsTableOnly { get; }
        public SqlAliasedFromSourceExpression FromSource { get; }
        public IReadOnlyList<SqlAliasedJoinSourceExpression> Joins { get; }
        public IReadOnlyList<SqlAliasedCteSourceExpression> CteDataSources { get; }
        public int? Top { get; }
        public bool IsDistinct { get; }
        public SqlSelectListExpression SelectColumnCollection { get; }
        public SqlFilterClauseExpression WhereClause { get; }
        public IReadOnlyList<SqlExpression> GroupByClause { get; }
        public SqlFilterClauseExpression HavingClause { get; }
        public SqlOrderByClauseExpression  OrderByClause { get; }
        public int? RowOffset { get; }
        public int? RowsPerPage { get; }
        public bool AutoProjection { get; }
        public string Tag { get; }
        public SqlExpression QueryShape { get; }
        public IReadOnlyCollection<SqlAliasedDataSourceExpression> AllDataSources { get; }

        public override SqlDataSourceQueryShapeExpression CreateQueryShape(Guid dataSourceAlias)
        {
            if (this.SelectColumnCollection?.SelectColumns.IsNullOrEmpty() ?? true)
                throw new InvalidOperationException($"Current Derived Table is '{this.NodeType}' and does not have a Select Column Collection.");

            if (this.SelectColumnCollection.SelectColumns.Count == 1 &&
                this.SelectColumnCollection.SelectColumns[0].ScalarColumn)
                return new SqlDataSourceQueryShapeExpression(new SqlDataSourceColumnExpression(dataSourceAlias, this.SelectColumnCollection.SelectColumns[0].Alias), dataSourceAlias);

            if (this.QueryShape is null)
                throw new InvalidOperationException($"QueryShape property is null.");

            // here we are NOT trying to re-create SqlSelectExpression we are trying to 
            // generate a query shape as if the projection has been applied on SqlSelectExpression
            // the shape will remain the same, that is nested member assignment, however, all the
            // data source expressions will be changed to use the new alias
            var queryShape = (this.QueryShape as SqlMemberInitExpression)
                                ??
                                (this.QueryShape as SqlDataSourceQueryShapeExpression)?.ShapeExpression as SqlMemberInitExpression
                                ??
                                throw new InvalidOperationException($"QueryShape property type is '{this.QueryShape.GetType().Name}' and is not a valid {nameof(SqlQueryShapeExpression)}.");

            var memberInit = this.UpdateQueryShapeWithNewAlias(queryShape, dataSourceAlias, this.SelectColumnCollection.SelectColumns);
            
            return new SqlDataSourceQueryShapeExpression(memberInit, dataSourceAlias);
        }

        public SqlQuerySourceExpression ConvertToTableIfPossible()
        {
            if (this.IsTableOnly)
                return this.FromSource.QuerySource;
            return this;
        }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlDerivedTable(this);
        }

        public SqlDerivedTableExpression Update(
            IReadOnlyList<SqlAliasedCteSourceExpression> cteDataSources,
            SqlAliasedFromSourceExpression fromSource,
            IReadOnlyList<SqlAliasedJoinSourceExpression> joinedDataSources,
            SqlFilterClauseExpression whereClause,
            IReadOnlyList<SqlExpression> groupByClause,
            SqlFilterClauseExpression havingClause,
            SqlOrderByClauseExpression orderByClause,
            SqlSelectListExpression selectColumnCollection)
        {
            if (this.CteDataSources.AllEqual(cteDataSources) &&
                this.FromSource == fromSource &&
                this.Joins.AllEqual(joinedDataSources) &&
                this.WhereClause == whereClause &&
                this.GroupByClause.AllEqual(groupByClause) &&
                this.HavingClause == havingClause &&
                this.OrderByClause == orderByClause &&
                this.SelectColumnCollection == selectColumnCollection)
                return this;

            return new SqlDerivedTableExpression(
                cteDataSources,
                fromSource,
                joinedDataSources,
                whereClause,
                groupByClause,
                havingClause,
                orderByClause,
                selectColumnCollection,
                this.IsDistinct,
                this.Top,
                this.RowOffset,
                this.RowsPerPage,
                this.AutoProjection,
                this.Tag,
                this.QueryShape,
                this.NodeType);
        }

        private string Indent(string value, string indentText = "\r\n\t")
        {
            return value.Replace("\r\n", indentText);
        }

        private string JoinInNewLine<T>(IEnumerable<T> values, string separator = "\r\n\t\t")
        {
            var result = string.Join("\r\n\t\t", values.Select(x => Indent(x.ToString(), "\r\n\t\t")));
            if (result.Length > 0)
                result = $"{separator}{result}";
            return result;
        }

        private string JoinPredicate(SqlFilterClauseExpression filterClause, string method)
        {
            if (filterClause is null)
                return string.Empty;

            var predicates = filterClause.FilterConditions;
            var predicatesToString = string.Join("\r\n\t\t", predicates.Select((x, i) => $"{(i > 0 ? (x.UseOrOperator ? " or " : " and ") : string.Empty)}{Indent(x.Predicate.ToString())}"));
            if (!string.IsNullOrEmpty(predicatesToString))
                predicatesToString = $"\r\n{method} {predicatesToString}";
            return predicatesToString;
        }

        private string CommaJoinMoveNextLine<T>(IEnumerable<T> values, string method)
        {
            var valuesToString = string.Join(", ", values.Select(x => Indent(x.ToString())));
            if (valuesToString.Length > 0)
                valuesToString = $"\r\n{method} {valuesToString}";
            return valuesToString;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var cteDataSourceToString = string.Join(", ", this.CteDataSources.Select(x => $"{DebugAliasGenerator.GetAlias(x.CteAlias)} as\r\n{Indent(x.CteBody.ToString())}"));
            if (cteDataSourceToString.Length > 0)
                cteDataSourceToString = $"with {cteDataSourceToString}\r\n";

            var fromString = $"\r\nfrom {Indent(this.FromSource.ToString())}";
            var joins = JoinInNewLine(this.Joins);
            var whereClause = JoinPredicate(this.WhereClause, "where");
            var groupByClause = CommaJoinMoveNextLine(this.GroupByClause, "group by");
            var havingClause = JoinPredicate(this.HavingClause, "having");
            var top = this.Top > 0 ? $" top {this.Top}" : string.Empty;
            var distinct = this.IsDistinct ? " distinct " : string.Empty;
            var selectList = this.SelectColumnCollection != null ? string.Join(", ", this.SelectColumnCollection.SelectColumns.Select(x => Indent(x.ToString()))) : null;
            string orderByClause;
            if (this.OrderByClause != null)
                orderByClause = CommaJoinMoveNextLine(this.OrderByClause.OrderByColumns, "order by");
            else
                orderByClause = string.Empty;
            string paging = string.Empty;
            if (RowOffset != null && RowsPerPage != null)
            {
                paging = $"\r\noffset {RowOffset} rows fetch next {RowsPerPage} rows only";
            }
            string selectClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(selectList))
            {
                selectClause = $"\tselect{distinct}{top} {selectList}";
            }
            var query = $"{cteDataSourceToString}{selectClause}{fromString}{joins}{whereClause}{groupByClause}{havingClause}{orderByClause}{paging}";
            query = $"(\r\n{Indent(query)}\r\n)";
            return query;
        }
    }
}
