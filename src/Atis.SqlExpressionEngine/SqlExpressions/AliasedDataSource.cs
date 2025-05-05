using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class AliasedDataSource
    {
        public AliasedDataSource(SqlQuerySourceExpression dataSource, Guid alias)
        {
            this.QuerySource = dataSource;
            this.Alias = alias;
        }
        public SqlQuerySourceExpression QuerySource { get; }
        public Guid Alias { get; }

        public override string ToString()
        {
            return $"{this.QuerySource} as {DebugAliasGenerator.GetAlias(this.Alias)}";
        }
    }

    //public class TableDataSource : AliasedDataSource
    //{
    //    public TableDataSource(SqlTableExpression dataSource, Guid alias) : base(dataSource, alias)
    //    {
    //    }

    //    public new SqlTableExpression DataSource => (SqlTableExpression)base.QuerySource;
    //}

    //public class DerivedTableDataSource : AliasedDataSource
    //{
    //    public DerivedTableDataSource(SqlDerivedTableExpression dataSource, Guid alias) : base(dataSource, alias)
    //    {
    //    }
    //    public new SqlDerivedTableExpression DataSource => (SqlDerivedTableExpression)base.QuerySource;
    //}

    public class CteDataSource
    {
        public CteDataSource(SqlSubQuerySourceExpression cteBody, Guid cteAlias) 
        {
            this.CteBody = cteBody;
            this.CteAlias = cteAlias;
        }
        public SqlSubQuerySourceExpression CteBody { get; }
        public Guid CteAlias { get; }

        public override string ToString()
        {
            return $"{this.CteBody} as {DebugAliasGenerator.GetAlias(this.CteAlias)}";
        }
    }

    public class JoinDataSource : AliasedDataSource
    {
        public JoinDataSource(SqlJoinType joinType, SqlQuerySourceExpression dataSource, Guid alias, SqlExpression joinCondition, string joinName, bool isNavigationJoin, Guid? navigationParent) 
            : base(dataSource, alias)
        {
            this.JoinType = joinType;
            this.JoinCondition = joinCondition;
            this.JoinName = joinName;
            this.IsNavigationJoin = isNavigationJoin;
            this.NavigationParent = navigationParent;
        }

        public SqlJoinType JoinType { get; }
        public SqlExpression JoinCondition { get; }
        public string JoinName { get; }
        public bool IsNavigationJoin { get; }
        public Guid? NavigationParent { get; }

        public override string ToString()
        {
            var joinCondition = this.JoinCondition != null ? $" on {this.JoinCondition}" : string.Empty;
            return $"{GetSqlJoinType(this.JoinType)} {this.QuerySource} as {DebugAliasGenerator.GetAlias(this.Alias)}{joinCondition}";
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
