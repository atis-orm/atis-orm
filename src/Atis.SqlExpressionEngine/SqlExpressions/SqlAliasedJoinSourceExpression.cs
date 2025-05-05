using Atis.SqlExpressionEngine.Internal;
using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlAliasedJoinSourceExpression : SqlAliasedDataSourceExpression
    {
        public SqlAliasedJoinSourceExpression(SqlJoinType joinType, SqlQuerySourceExpression querySource, Guid dataSourceAlias, SqlExpression joinCondition, string joinName, bool isNavigationJoin, Guid? navigationParent)
        {
            if (querySource is null)
                throw new ArgumentNullException(nameof(querySource));
            this.QuerySource = querySource ?? throw new ArgumentNullException(nameof(querySource));
            this.Alias = dataSourceAlias;
            this.JoinType = joinType;
            this.JoinCondition = joinCondition;
            this.JoinName = joinName;
            this.IsNavigationJoin = isNavigationJoin;
            this.NavigationParent = navigationParent;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.JoinDataSource;
        public SqlExpression JoinCondition { get; }
        public override SqlQuerySourceExpression QuerySource { get; }
        public override Guid Alias { get; }
        public SqlJoinType JoinType { get; }
        public string JoinName { get; }
        public bool IsNavigationJoin { get; }
        public Guid? NavigationParent { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlAliasedJoinSource(this);
        }

        public SqlAliasedJoinSourceExpression Update(SqlQuerySourceExpression querySource, SqlExpression joinCondition)
        {
            if (querySource == this.QuerySource && joinCondition == this.JoinCondition)
                return this;
            return new SqlAliasedJoinSourceExpression(this.JoinType, querySource, this.Alias, joinCondition, this.JoinName, this.IsNavigationJoin, this.NavigationParent);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var alias = DebugAliasGenerator.GetAlias(this);
            var joinCondition = this.JoinCondition != null ? $" on {this.JoinCondition}" : string.Empty;
            return $"{GetSqlJoinType(this.JoinType)} {this.QuerySource} as {alias}{joinCondition}";
        }

        private static string GetSqlJoinType(SqlJoinType joinType)
        {
            switch (joinType)
            {
                case SqlJoinType.Inner:
                    return "inner join";
                case SqlJoinType.Left:
                    return "left join";
                case SqlJoinType.Right:
                    return "right join";
                case SqlJoinType.Cross:
                    return "cross join";
                case SqlJoinType.OuterApply:
                    return "outer apply";
                case SqlJoinType.CrossApply:
                    return "cross apply";
                case SqlJoinType.FullOuter:
                    return "full outer join";
                default:
                    return joinType.ToString();
            }
        }
    }
}
