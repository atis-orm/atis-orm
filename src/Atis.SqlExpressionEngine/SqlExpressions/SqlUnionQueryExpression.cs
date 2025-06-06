﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public enum SqlUnionType
    {
        Union,
        UnionAll
    }

    public class UnionItem
    {
        public UnionItem(SqlDerivedTableExpression derivedTable, SqlUnionType unionAll)
        {
            this.DerivedTable = derivedTable ?? throw new ArgumentNullException(nameof(derivedTable));
            this.UnionType = unionAll;
        }
        public SqlDerivedTableExpression DerivedTable { get; }
        public SqlUnionType UnionType { get; }
    }

    public class SqlUnionQueryExpression : SqlSubQuerySourceExpression
    {
        public SqlUnionQueryExpression(IReadOnlyList<UnionItem> unions)
        {
            if (unions is null || unions.Count <= 1)
                throw new ArgumentException("Minimum 2 items are required.", nameof(unions));
            this.Unions = unions;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Union;
        public IReadOnlyList<UnionItem> Unions { get; }

        /// <inheritdoc />
        public override SqlDataSourceQueryShapeExpression CreateQueryShape(Guid dataSourceAlias)
        {
            return this.Unions.First().DerivedTable.CreateQueryShape(dataSourceAlias);
        }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitUnionQuery(this);
        }

        public SqlUnionQueryExpression Update(IReadOnlyList<UnionItem> unionItems)
        {
            if (this.Unions.AllEqual(unionItems))
                return this;
            return new SqlUnionQueryExpression(unionItems);
        }

        private string ConvertUnionItemToString(UnionItem unionItem, bool prependUnionKeyword)
        {
            string unionKeyword;
            if (prependUnionKeyword)
            {
                if (unionItem.UnionType == SqlUnionType.UnionAll)
                    unionKeyword = "\tunion all\r\n";
                else
                    unionKeyword = "\tunion\r\n";
            }
            else
                unionKeyword = string.Empty;
            var derivedTable = unionItem.DerivedTable.ToString();
            if (derivedTable.StartsWith("("))
                derivedTable = derivedTable.Substring(1, derivedTable.Length - 2).Trim();
            return $"{unionKeyword}\t{derivedTable}";
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var unions = this.Unions.Select((x, i) => this.ConvertUnionItemToString(x, i > 0));
            return $"(\r\n{string.Join("\r\n", unions)}\r\n)";
        }
    }
}
