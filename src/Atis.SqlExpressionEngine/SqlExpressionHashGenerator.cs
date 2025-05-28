using Atis.SqlExpressionEngine.SqlExpressions;
using System;

namespace Atis.SqlExpressionEngine
{
    public class SqlExpressionHashGenerator : SqlExpressionVisitor
    {
        private HashCode hashCode;

        /// <inheritdoc />
        public override SqlExpression Visit(SqlExpression node)
        {
            if (node is null)
                this.hashCode.Add(0);
            else
                this.hashCode.Add(node.NodeType);
            return base.Visit(node);
        }

        public static int GenerateHash(SqlExpression sqlExpression)
        {
            if (sqlExpression is null)
                throw new ArgumentNullException(nameof(sqlExpression));
            var hashGenerator = new SqlExpressionHashGenerator();
            return hashGenerator.Generate(sqlExpression);
        }

        public int Generate(SqlExpression expression)
        {
            this.hashCode = new HashCode();
            Visit(expression);
            return hashCode.ToHashCode();
        }

        protected internal override SqlExpression VisitSqlLiteral(SqlLiteralExpression sqlLiteralExpression)
        {
            if (sqlLiteralExpression.LiteralValue == null)
                this.hashCode.Add(0);
            else
                this.hashCode.Add(sqlLiteralExpression.LiteralValue);
            return base.VisitSqlLiteral(sqlLiteralExpression);
        }

        protected internal override SqlExpression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
        {
            // TODO: might need to add Type as well along with value in SqlParameterExpression
            if (sqlParameterExpression.Value == null)
                this.hashCode.Add(0);
            else
                this.hashCode.Add(sqlParameterExpression.Value.GetType());
            return base.VisitSqlParameter(sqlParameterExpression);
        }

        protected internal override SqlExpression VisitSqlFunctionCall(SqlFunctionCallExpression node)
        {
            this.hashCode.Add(node.FunctionName);
            return base.VisitSqlFunctionCall(node);
        }

        protected internal override SqlExpression VisitSqlAlias(SqlAliasExpression node)
        {
            this.hashCode.Add(node.ColumnAlias);
            return base.VisitSqlAlias(node);
        }

        protected internal override SqlExpression VisitSqlDelete(SqlDeleteExpression node)
        {
            this.hashCode.Add(node.DataSourceAlias);
            return base.VisitSqlDelete(node);
        }

        protected internal override SqlExpression VisitSqlStandaloneSelect(SqlStandaloneSelectExpression node)
        {
            return base.VisitSqlStandaloneSelect(node);
        }

        protected internal override SqlExpression VisitSqlAliasedCteSource(SqlAliasedCteSourceExpression node)
        {
            this.hashCode.Add(node.CteAlias);
            return base.VisitSqlAliasedCteSource(node);
        }

        protected internal override SqlExpression VisitSqlAliasedFromSource(SqlAliasedFromSourceExpression node)
        {
            this.hashCode.Add(node.Alias);
            return base.VisitSqlAliasedFromSource(node);
        }

        protected internal override SqlExpression VisitSqlAliasedJoinSource(SqlAliasedJoinSourceExpression node)
        {
            this.hashCode.Add(node.Alias);
            this.hashCode.Add(node.JoinName);
            this.hashCode.Add(node.JoinType);
            this.hashCode.Add(node.IsNavigationJoin);
            return base.VisitSqlAliasedJoinSource(node);
        }

        protected internal override SqlExpression VisitSqlCast(SqlCastExpression node)
        {
            this.hashCode.Add(node.SqlDataType);
            return base.VisitSqlCast(node);
        }

        protected internal override SqlExpression VisitSqlDataSourceColumn(SqlDataSourceColumnExpression node)
        {
            this.hashCode.Add(node.DataSourceAlias);
            this.hashCode.Add(node.ColumnName);
            return base.VisitSqlDataSourceColumn(node);
        }

        protected internal override SqlExpression VisitSqlDateAdd(SqlDateAddExpression node)
        {
            this.hashCode.Add(node.DatePart);
            return base.VisitSqlDateAdd(node);
        }

        protected internal override SqlExpression VisitSqlDatePart(SqlDatePartExpression node)
        {
            this.hashCode.Add(node.DatePart);
            return base.VisitSqlDatePart(node);
        }

        protected internal override SqlExpression VisitSqlDateSubtract(SqlDateSubtractExpression node)
        {
            this.hashCode.Add(node.DatePart);
            return base.VisitSqlDateSubtract(node);
        }

        protected internal override SqlExpression VisitSqlDerivedTable(SqlDerivedTableExpression node)
        {
            this.hashCode.Add(node.AutoProjection);
            this.hashCode.Add(node.IsCte);
            this.hashCode.Add(node.IsDistinct);
            this.hashCode.Add(node.RowOffset);
            this.hashCode.Add(node.RowsPerPage);
            this.hashCode.Add(node.Tag);
            this.hashCode.Add(node.Top);
            return base.VisitSqlDerivedTable(node);
        }

        protected internal override SqlExpression VisitSqlSelectList(SqlSelectListExpression node)
        {
            foreach (var selectItem in node.SelectColumns)
            {
                this.hashCode.Add(selectItem.Alias);
                this.hashCode.Add(selectItem.ScalarColumn);
            }
            return base.VisitSqlSelectList(node);
        }

        protected internal override SqlExpression VisitSqlStringFunction(SqlStringFunctionExpression node)
        {
            this.hashCode.Add(node.StringFunction);
            return base.VisitSqlStringFunction(node);
        }

        protected internal override SqlExpression VisitSqlTable(SqlTableExpression node)
        {
            this.hashCode.Add(node.SqlTable);
            foreach (var column in node.TableColumns)
            {
                this.hashCode.Add(column.DatabaseColumnName);
                this.hashCode.Add(column.ModelPropertyName);
            }
            return base.VisitSqlTable(node);
        }

        protected internal override SqlExpression VisitSqlUpdate(SqlUpdateExpression node)
        {
            foreach (var column in node.Columns)
            {
                this.hashCode.Add(column);
            }
            this.hashCode.Add(node.DataSource);
            return base.VisitSqlUpdate(node);
        }

        protected internal override SqlExpression VisitSqlComment(SqlCommentExpression node)
        {
            this.hashCode.Add(node.Comment);
            return base.VisitSqlComment(node);
        }

        protected internal override SqlExpression VisitSqlFragment(SqlFragmentExpression node)
        {
            this.hashCode.Add(node.Fragment);
            return base.VisitSqlFragment(node);
        }

        protected internal override SqlExpression VisitDataSourceQueryShape(SqlDataSourceQueryShapeExpression node)
        {
            this.hashCode.Add(node.DataSourceAlias);
            return base.VisitDataSourceQueryShape(node);
        }
    }
}
