using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.SqlExpressions;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public class SqlExpressionTranslator
    {
        private Dictionary<Guid, string> aliasCache = new Dictionary<Guid, string>();
        private Dictionary<Guid, int> expressionIdCache = new Dictionary<Guid, int>();

        private string GetSimpleAlias(Guid aliasGuid, string? prefix = null)
        {
            if (!this.aliasCache.TryGetValue(aliasGuid, out var alias))
            {
                alias = $"{(prefix ?? "a")}_{this.aliasCache.Count + 1}";
                this.aliasCache.Add(aliasGuid, alias);
            }
            return alias;
        }

        public string Translate(SqlExpression sqlExpression)
        {
            if (sqlExpression is SqlDerivedTableExpression sqlDerivedTableExpression)
            {
                return this.TranslateSqlDerivedTableExpression(sqlDerivedTableExpression);
            }
            else if (sqlExpression is SqlAliasedJoinSourceExpression sqlAliasedJoinSourceExpression)
            {
                return this.TranslateSqlAliasedJoinSourceExpression(sqlAliasedJoinSourceExpression);
            }
            else if (sqlExpression is SqlAliasedFromSourceExpression sqlAliasedFromSourceExpression)
            {
                return this.TranslateSqlAliasedFromSourceExpression(sqlAliasedFromSourceExpression);
            }
            else if (sqlExpression is SqlBinaryExpression sqlBinaryExpression)
            {
                return this.TranslateSqlBinaryExpression(sqlBinaryExpression);
            }
            else if (sqlExpression is SqlLiteralExpression sqlLiteralExpression)
            {
                return this.TranslateSqlLiteralExpression(sqlLiteralExpression);
            }
            else if (sqlExpression is SqlCollectionExpression sqlCollectionExpression)
            {
                return this.TranslateSqlCollectionExpression(sqlCollectionExpression);
            }
            else if (sqlExpression is SqlExistsExpression sqlExistsExpression)
            {
                return this.TranslateSqlExistsExpression(sqlExistsExpression);
            }
            else if (sqlExpression is SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
            {
                return this.TranslateSqlDataSourceColumnExpression(sqlDataSourceColumnExpression);
            }
            else if (sqlExpression is SqlFunctionCallExpression sqlFunctionCallExpression)
            {
                return this.TranslateSqlFunctionCallExpression(sqlFunctionCallExpression);
            }
            else if (sqlExpression is SqlParameterExpression sqlParameterExpression)
            {
                return this.TranslateSqlParameterExpression(sqlParameterExpression);
            }
            else if (sqlExpression is SqlTableExpression sqlTableExpression)
            {
                return this.TranslateSqlTableExpression(sqlTableExpression);
            }
            else if (sqlExpression is SqlAliasExpression sqlAliasExpression)
            {
                return this.TranslateSqlAliasExpression(sqlAliasExpression);
            }
            else if (sqlExpression is SqlCteReferenceExpression sqlCteExpression)
            {
                return this.TranslateSqlCteReferenceExpression(sqlCteExpression);
            }
            else if (sqlExpression is SqlConditionalExpression sqlConditionExpression)
            {
                return this.TranslateSqlConditionalExpression(sqlConditionExpression);
            }
            else if (sqlExpression is SqlUpdateExpression sqlUpdateExpression)
            {
                return this.TranslateSqlUpdateExpression(sqlUpdateExpression);
            }
            else if (sqlExpression is SqlDeleteExpression sqlDeleteExpression)
            {
                return this.TranslateSqlDeleteExpression(sqlDeleteExpression);
            }
            else if (sqlExpression is SqlNotExpression sqlNotExpression)
            {
                return this.TranslateSqlNotExpression(sqlNotExpression);
            }
            else if (sqlExpression is SqlInValuesExpression sqlInValuesExpression)
            {
                return this.TranslateSqlInValuesExpression(sqlInValuesExpression);
            }
            //else if (sqlExpression is SqlKeywordExpression sqlKeywordExpression)
            //{
            //    return this.TranslateSqlKeywordExpression(sqlKeywordExpression);
            //}
            else if (sqlExpression is SqlNegateExpression sqlNegateExpression)
            {
                return this.TranslateSqlNegateExpression(sqlNegateExpression);
            }
            else if (sqlExpression is SqlCastExpression sqlCastExpression)
            {
                return this.TranslateSqlCastExpression(sqlCastExpression);
            }
            else if (sqlExpression is SqlDateAddExpression sqlDateAddExpression)
            {
                return this.TranslateSqlDateAddExpression(sqlDateAddExpression);
            }
            else if (sqlExpression is SqlDatePartExpression sqlDatePartExpression)
            {
                return this.TranslateSqlDatePartExpression(sqlDatePartExpression);
            }
            else if (sqlExpression is SqlDateSubtractExpression sqlDateSubtractExpression)
            {
                return this.TranslateSqlSubtractExpression(sqlDateSubtractExpression);
            }
            else if (sqlExpression is SqlStringFunctionExpression sqlStringFunction)
            {
                return this.TranslateSqlStringFunctionExpression(sqlStringFunction);
            }
            else if (sqlExpression is SqlLikeExpression sqlLikeExpression)
            {
                return this.TranslateSqlLikeExpression(sqlLikeExpression);
            }
            else if (sqlExpression is SqlUnionQueryExpression sqlUnionQueryExpression)
            {
                return this.TranslateSqlUnionQueryExpression(sqlUnionQueryExpression);
            }
            else if (sqlExpression is SqlStandaloneSelectExpression sqlStandaloneSelectExpression)
            {
                return this.TranslateSqlStandaloneSelectExpression(sqlStandaloneSelectExpression);
            }
            else if (sqlExpression is SqlQueryableExpression sqlQueryableExpression)
            {
                return this.TranslateSqlQueryableExpression(sqlQueryableExpression);
            }
            else if (sqlExpression is SqlCommentExpression sqlComment)
            {
                return this.TranslateSqlCommentExpression(sqlComment);
            }
            else
            {
                throw new NotSupportedException($"SqlExpression type '{sqlExpression?.GetType().Name}' is not supported.");
            }
        }

        private string TranslateSqlCommentExpression(SqlCommentExpression sqlComment)
        {
            return $"/*{sqlComment.Comment}*/";
        }

        private string TranslateSqlQueryableExpression(SqlQueryableExpression sqlQueryableExpression)
        {
            return $"Queryable: {{\r\n{Indent(this.Translate(sqlQueryableExpression.Query))}\r\n}}";
        }

        private string TranslateSqlCollectionExpression(SqlCollectionExpression sqlCollectionExpression)
        {
            return string.Join(", ", sqlCollectionExpression.SqlExpressions.Select(this.Translate));
        }

        private string TranslateSqlStandaloneSelectExpression(SqlStandaloneSelectExpression node)
        {
            var selectColumnToString = string.Join(", ", node.SelectList.Select(x => this.TranslateSelectColumn(x)));
            return $"(select {selectColumnToString})";
        }

        private string TranslateSqlAliasedFromSourceExpression(SqlAliasedFromSourceExpression node)
        {
            return $"{this.Translate(node.QuerySource)} as {this.GetSimpleAlias(node.Alias)}";
        }

        private string TranslateSqlAliasedJoinSourceExpression(SqlAliasedJoinSourceExpression node)
        {
            var alias = this.GetSimpleAlias(node.Alias, node.JoinName);
            var joinCondition = node.JoinCondition != null ? $" on {this.TranslateLogicalExpression(node.JoinCondition)}" : string.Empty;
            return $"{GetSqlJoinType(node.JoinType)} {this.Translate(node.QuerySource)} as {alias}{joinCondition}";
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
            var derivedTable = this.Translate(unionItem.DerivedTable);
            if (derivedTable.StartsWith("("))
                derivedTable = derivedTable.Substring(1, derivedTable.Length - 2).Trim();
            return $"{unionKeyword}\t{derivedTable}";
        }

        private string TranslateSqlUnionQueryExpression(SqlUnionQueryExpression node)
        {
            var unions = node.Unions.Select((x, i) => this.ConvertUnionItemToString(x, i > 0));
            return $"(\r\n{string.Join("\r\n", unions)}\r\n)";
        }

        private string Indent(string value, string indentText = "\r\n\t")
        {
            return value.Replace("\r\n", indentText);
        }

        private string JoinInNewLine(IEnumerable<SqlAliasedJoinSourceExpression> joins, string separator = "\r\n\t\t")
        {
            var result = string.Join("\r\n\t\t", joins.Select(x => Indent(this.Translate(x), "\r\n\t\t")));
            if (result.Length > 0)
                result = $"{separator}{result}";
            return result;
        }

        private string JoinPredicate(SqlFilterClauseExpression filterClause, string method)
        {
            if (filterClause is null || filterClause.FilterConditions.Length == 0)
                return string.Empty;

            var predicateString = new StringBuilder();

            for (var i = 0; i < filterClause.FilterConditions.Length; i++)
            {
                var isLastCondition = i == filterClause.FilterConditions.Length - 1;
                var condition = filterClause.FilterConditions[i];

                var translated = Indent(this.TranslateLogicalExpression(condition.Predicate));

                var hasOrOperatorInThis = condition.UseOrOperator;
                var hasOrOperatorInNext = !isLastCondition && filterClause.FilterConditions[i + 1].UseOrOperator;

                if (i == 0)
                {
                    if (hasOrOperatorInNext)                                // 0 == 1 or orCondition1
                        predicateString.Append('(');
                    predicateString.Append(translated);
                }
                else
                {
                    predicateString.Append(condition.UseOrOperator ? " or " : Indent("\r\n and "));

                    // e.g.  condition1 and 0 == 1 or orCondition1 or orCondition2 or orCondition3 and condition2 and condition3

                    if (!hasOrOperatorInThis && hasOrOperatorInNext)        // and 0 == 1
                        predicateString.Append('(');

                    predicateString.Append(translated);

                    if (hasOrOperatorInThis && !hasOrOperatorInNext)        // or orCondition3
                        predicateString.Append(')');
                }
            }

            return $"\r\n{method} {predicateString}";
        }


        private string CommaJoinMoveNextLine<T>(IEnumerable<T> values, string method, Func<T, string> translateMethod)
        {
            var valuesToString = string.Join(", ", values.Select(x => Indent(translateMethod(x))));
            if (valuesToString.Length > 0)
                valuesToString = $"\r\n{method} {valuesToString}";
            return valuesToString;
        }

        private string TranslateOrderByColumn(OrderByColumn orderByColumn)
        {
            return $"{this.Translate(orderByColumn.ColumnExpression)} {(orderByColumn.Direction == SortDirection.Ascending ? "asc" : "desc")}";
        }

        private string TranslateSelectColumn(SelectColumn selectColumn)
        {
            return $"{this.TranslateNonLogicalExpression(selectColumn.ColumnExpression)} as {selectColumn.Alias}";
        }

        private string TranslateSelectColumns(SelectColumn[] selectColumns)
        {
            var result = new StringBuilder();
            for(var i = 0; i < selectColumns.Length; i++)
            {
                var selectColumn = selectColumns[i];
                var comment = selectColumn.ColumnExpression as SqlCommentExpression;
                if (comment != null)
                    result.Append(" /*");
                if (i > 0)
                    result.Append(", ");

                if (comment is null)
                    result.Append(Indent(this.TranslateSelectColumn(selectColumn)));
                else
                    result.Append(comment.Comment).Append(" as ").Append(selectColumn.Alias);

                if (comment != null)
                    result.Append("*/");
            }
            return result.ToString();
        }


        private string TranslateSqlDerivedTableExpression(SqlDerivedTableExpression node)
        {
            var cteDataSourceToString = string.Join(", ", node.CteDataSources.Select(x => $"{this.GetSimpleAlias(x.CteAlias, "cte")} as\r\n{this.Translate(x.CteBody)}"));
            if (cteDataSourceToString.Length > 0)
                cteDataSourceToString = $"\twith {cteDataSourceToString}\r\n";
            else
                cteDataSourceToString = "\t";

            var fromString = $"\r\nfrom {Indent(this.Translate(node.FromSource))}";
            var joins = JoinInNewLine(node.Joins);
            var whereClause = JoinPredicate(node.WhereClause, "where");
            var groupByClause = CommaJoinMoveNextLine(node.GroupByClause, "group by", this.Translate);
            var havingClause = JoinPredicate(node.HavingClause, "having");
            var top = node.Top > 0 ? $" top ({node.Top})" : string.Empty;
            var distinct = node.IsDistinct ? " distinct " : string.Empty;
            string? selectList = null;
            if (node.SelectColumnCollection != null)
                selectList = this.TranslateSelectColumns(node.SelectColumnCollection.SelectColumns);
            string orderByClause;
            if (node.OrderByClause != null)
                orderByClause = CommaJoinMoveNextLine(node.OrderByClause.OrderByColumns, "order by", this.TranslateOrderByColumn);
            else
                orderByClause = string.Empty;
            string paging = string.Empty;
            if (node.RowOffset != null && node.RowsPerPage != null)
            {
                paging = $"\r\noffset {node.RowOffset} rows fetch next {node.RowsPerPage} rows only";
            }
            string selectClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(selectList))
            {
                selectClause = $"select{distinct}{top} {selectList}";
            }
            var query = $"{cteDataSourceToString}{selectClause}{fromString}{joins}{whereClause}{groupByClause}{havingClause}{orderByClause}{paging}";
            query = $"(\r\n{Indent(query)}\r\n)";
            return query;
        }

        private string TranslateSqlLikeExpression(SqlLikeExpression sqlLikeExpression)
        {
            switch (sqlLikeExpression.NodeType)
            {
                case SqlExpressionType.LikeStartsWith:
                    return $"({this.Translate(sqlLikeExpression.Expression)} like {this.Translate(sqlLikeExpression.Pattern)} + '%')";
                case SqlExpressionType.LikeEndsWith:
                    return $"({this.Translate(sqlLikeExpression.Expression)} like '%' + {this.Translate(sqlLikeExpression.Pattern)})";
                default:
                    return $"({this.Translate(sqlLikeExpression.Expression)} like '%' + {this.Translate(sqlLikeExpression.Pattern)} + '%')";
            }
        }

        private string TranslateSqlStringFunctionExpression(SqlStringFunctionExpression sqlStringFunctionExpression)
        {
            string arguments = string.Empty;
            if (sqlStringFunctionExpression.Arguments?.Length > 0)
                arguments = $", {string.Join(", ", sqlStringFunctionExpression.Arguments.Select(this.Translate))}";
            return $"{sqlStringFunctionExpression.StringFunction}({this.Translate(sqlStringFunctionExpression.StringExpression)}{arguments})";
        }

        private string TranslateSqlDatePartExpression(SqlDatePartExpression sqlDatePartExpression)
        {
            return $"datePart({sqlDatePartExpression.DatePart}, {this.Translate(sqlDatePartExpression.DateExpression)})";
        }

        private string TranslateSqlDateAddExpression(SqlDateAddExpression sqlDateAddExpression)
        {
            return $"dateAdd({sqlDateAddExpression.DatePart}, {this.Translate(sqlDateAddExpression.Interval)}, {this.Translate(sqlDateAddExpression.DateExpression)})";
        }

        private string TranslateSqlSubtractExpression(SqlDateSubtractExpression sqlDateSubtractExpression)
        {
            return $"dateSubtract({sqlDateSubtractExpression.DatePart}, {this.Translate(sqlDateSubtractExpression.StartDate)}, {this.Translate(sqlDateSubtractExpression.EndDate)})";
        }

        private string TranslateSqlCastExpression(SqlCastExpression sqlCastExpression)
        {
            return $"cast({this.Translate(sqlCastExpression.Expression)} as {this.TranslateSqlDataType(sqlCastExpression.SqlDataType)})";
        }

        private string TranslateSqlDataType(ISqlDataType sqlDataType)
        {
            string length;
            if (sqlDataType.UseMaxLength)
            {
                length = "(max)";
            }
            else
            {
                length = sqlDataType.Length != null ? $"({sqlDataType.Length})" : string.Empty;
            }
            string decimalParams = sqlDataType.Precision != null && sqlDataType.Scale != null ? $"({sqlDataType.Precision}, {sqlDataType.Scale})" : string.Empty;
            return $"{sqlDataType.DbType}{length}{decimalParams}";
        }

        private string TranslateSqlNegateExpression(SqlNegateExpression sqlNegateExpression)
        {
            return $"-{this.Translate(sqlNegateExpression.Operand)}";
        }

        private string TranslateSqlNotExpression(SqlNotExpression sqlNotExpression)
        {
            var operand = this.TranslateLogicalExpression(sqlNotExpression.Operand);
            return $"not {operand}";
        }

        //private string TranslateSqlKeywordExpression(SqlKeywordExpression sqlKeywordExpression)
        //{
        //    return sqlKeywordExpression.Keyword;
        //}

        private string TranslateSqlInValuesExpression(SqlInValuesExpression sqlInValuesExpression)
        {
            var expressionTranslated = this.Translate(sqlInValuesExpression.Expression);
            var valuesTranslated = string.Join(", ", sqlInValuesExpression.Values.Select(this.Translate));
            return $"{expressionTranslated} in ({valuesTranslated})";
        }

        private string TranslateSqlUpdateExpression(SqlUpdateExpression sqlUpdateExpression)
        {
            var updateColumns = sqlUpdateExpression.Columns.Zip(sqlUpdateExpression.Values, (c, v) => $"{c} = {this.Translate(v)}");
            var sqlQuery = this.Translate(sqlUpdateExpression.Source);
            sqlQuery = this.RemoveParenthesis(sqlQuery);
            var query = $"update {this.GetSimpleAlias(sqlUpdateExpression.DataSource)}\r\n\tset {string.Join(",\r\n\t\t", updateColumns)}\r\n{sqlQuery}";
            return query;
        }

        private string TranslateSqlDeleteExpression(SqlDeleteExpression sqlDeleteExpression)
        {
            var sqlQuery = this.Translate(sqlDeleteExpression.Source);
            sqlQuery = this.RemoveParenthesis(sqlQuery);
            var query = $"delete {this.GetSimpleAlias(sqlDeleteExpression.DataSourceAlias)}\r\n{sqlQuery}";
            return query;
        }

        private string TranslateSqlConditionalExpression(SqlConditionalExpression sqlConditionExpression)
        {
            var test = this.TranslateLogicalExpression(sqlConditionExpression.Test);
            var ifTrue = this.TranslateNonLogicalExpression(sqlConditionExpression.IfTrue);
            var ifFalse = this.TranslateNonLogicalExpression(sqlConditionExpression.IfFalse);
            return $"case when {test} then {ifTrue} else {ifFalse} end";
        }

        private string TranslateSqlCteReferenceExpression(SqlCteReferenceExpression sqlCteExpression)
        {
            return this.GetSimpleAlias(sqlCteExpression.CteAlias, "cte");
        }

        private string TranslateSqlAliasExpression(SqlAliasExpression sqlAliasExpression)
        {
            return sqlAliasExpression.ColumnAlias;
        }

        private string TranslateSqlTableExpression(SqlTableExpression sqlTableExpression)
        {
            return sqlTableExpression.TableName;
        }

        private string TranslateLogicalExpression(SqlExpression sqlExpression)
        {
            if (!this.IsLogicalExpression(sqlExpression))
            {
                sqlExpression = new SqlBinaryExpression(sqlExpression, new SqlLiteralExpression(true), SqlExpressionType.Equal);
            }
            return this.Translate(sqlExpression);
        }

        private string TranslateNonLogicalExpression(SqlExpression sqlExpression)
        {
            if (this.IsLogicalExpression(sqlExpression))
            {
                sqlExpression = new SqlConditionalExpression(sqlExpression, new SqlLiteralExpression(true), new SqlLiteralExpression(false));
            }
            return this.Translate(sqlExpression);
        }


        int paramCount = 0;
        private string TranslateSqlParameterExpression(SqlParameterExpression sqlParameterExpression)
        {
            //return $"@p{paramCount++}";
            return SqlParameterExpression.ConvertObjectToString(sqlParameterExpression.Value);
        }

        private string JoinTypeToString(SqlJoinType joinType)
        {
            string joinTypeString;
            switch (joinType)
            {
                case SqlJoinType.Left:
                    joinTypeString = "left join";
                    break;
                case SqlJoinType.Right:
                    joinTypeString = "right join";
                    break;
                case SqlJoinType.Inner:
                    joinTypeString = "inner join";
                    break;
                case SqlJoinType.Cross:
                    joinTypeString = "cross join";
                    break;
                case SqlJoinType.OuterApply:
                    joinTypeString = "outer apply";
                    break;
                case SqlJoinType.CrossApply:
                    joinTypeString = "cross apply";
                    break;
                case SqlJoinType.FullOuter:
                    joinTypeString = "full outer join";
                    break;
                default:
                    joinTypeString = joinType.ToString();
                    break;
            }

            return joinTypeString;
        }

        private string TranslateSqlFunctionCallExpression(SqlFunctionCallExpression sqlFunctionCallExpression)
        {
            var arguments = sqlFunctionCallExpression.Arguments != null ? this.TranslateSqlFunctionArguments(sqlFunctionCallExpression) : string.Empty;
            if (sqlFunctionCallExpression.FunctionName == "Count")
            {
                arguments = string.IsNullOrWhiteSpace(arguments) ? "1" : arguments;
            }
            return $"{sqlFunctionCallExpression.FunctionName}({arguments})";
        }

        private string TranslateSqlFunctionArguments(SqlFunctionCallExpression sqlFunctionCallExpression)
        {
            var translation = new StringBuilder();
            foreach (var arg in sqlFunctionCallExpression.Arguments)
            {
                if (translation.Length > 0)
                {
                    translation.Append(", ");
                }
                var translated = this.TranslateNonLogicalExpression(arg);
                translation.Append(translated);
            }
            return translation.ToString();
        }


        private string TranslateSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
        {
            return $"{this.GetSimpleAlias(sqlDataSourceColumnExpression.DataSourceAlias)}.{sqlDataSourceColumnExpression.ColumnName}";
        }

        private string TranslateSqlExistsExpression(SqlExistsExpression sqlExistsExpression)
        {
            var sqlQuery = this.Translate(sqlExistsExpression.SubQuery);
            return $"exists{sqlQuery}";
        }

        private string TranslateSqlLiteralExpression(SqlLiteralExpression sqlLiteralExpression)
        {
            return SqlParameterExpression.ConvertObjectToString(sqlLiteralExpression.LiteralValue);
        }

        private bool IsNull(SqlExpression sqlExpression)
        {
            if (sqlExpression is SqlLiteralExpression sqlLiteralExpression)
            {
                return sqlLiteralExpression.LiteralValue == null;
            }
            else if (sqlExpression is SqlParameterExpression sqlParameterExpression)
            {
                return sqlParameterExpression.Value == null;
            }
            else
            {
                return false;
            }
        }

        private string TranslateSqlBinaryExpression(SqlBinaryExpression sqlBinaryExpression)
        {
            if (sqlBinaryExpression.NodeType != SqlExpressionType.Coalesce)
            {
                if (IsNull(sqlBinaryExpression.Right))
                {
                    if (sqlBinaryExpression.NodeType == SqlExpressionType.Equal)
                        return $"({this.Translate(sqlBinaryExpression.Left)} is null)";
                    else if (sqlBinaryExpression.NodeType == SqlExpressionType.NotEqual)
                        return $"({this.Translate(sqlBinaryExpression.Left)} is not null)";
                }

                string left;
                string right;

                if (sqlBinaryExpression.NodeType == SqlExpressionType.AndAlso || sqlBinaryExpression.NodeType == SqlExpressionType.OrElse)
                {
                    left = this.TranslateLogicalExpression(sqlBinaryExpression.Left);
                    right = this.TranslateLogicalExpression(sqlBinaryExpression.Right);
                }
                else
                {
                    // if we are here then it means binary expression =, !=, >, >=, <, <=, +, -, *, /, % or bitwise operator
                    // if this is the case then this is a must that left and right cannot be further And / OR
                    // e.g      left > right        (here left/right cannot be `5 > 4 and 5 < 6`, `1 && 6`)
                    // left and right will be some type of non-binary operation
                    // in-case of equal and not equal they should be handling the null case
                    // e.g.     a_1.Field1 = a_2.Field2
                    // should be translated to
                    //          a_1.Field1 = a_2.Field2 or (a_1.Field1 is null and a_2.Field2 is null)
                    // similarly
                    //          a_1.Field1 != a_2.Field2
                    // should be translated to
                    //          a_1.Field1 != a_2.Field2 or ((a_1.Field1 is null and a_2.Field2 is not null) or (a_1.Field1 is not null and a_2.Field2 is null))

                    left = this.TranslateNonLogicalExpression(sqlBinaryExpression.Left);
                    right = this.TranslateNonLogicalExpression(sqlBinaryExpression.Right);
                }

                var op = GetOperator(sqlBinaryExpression.NodeType);

                return $"({left} {op} {right})";
            }
            else
            {
                var left = this.TranslateNonLogicalExpression(sqlBinaryExpression.Left);
                var right = this.TranslateNonLogicalExpression(sqlBinaryExpression.Right);
                return $"isNull({left}, {right})";
            }
        }

        private bool IsLogicalExpression(SqlExpression sqlExpression)
        {
            var nt = sqlExpression.NodeType;
            if (nt == SqlExpressionType.AndAlso || nt == SqlExpressionType.OrElse ||
                nt == SqlExpressionType.GreaterThan || nt == SqlExpressionType.GreaterThanOrEqual ||
                nt == SqlExpressionType.LessThan || nt == SqlExpressionType.LessThanOrEqual ||
                nt == SqlExpressionType.Equal || nt == SqlExpressionType.NotEqual ||
                nt == SqlExpressionType.Like || nt == SqlExpressionType.LikeStartsWith || nt == SqlExpressionType.LikeEndsWith ||
                nt == SqlExpressionType.InValues ||
                nt == SqlExpressionType.Not || nt == SqlExpressionType.Exists
                )
                return true;

            return false;
        }

        private string RemoveParenthesis(string expression)
        {
            if (expression.StartsWith("(") && expression.EndsWith(")"))
            {
                return expression.Substring(1, expression.Length - 2).Trim();
            }
            return expression;
        }

        private static string GetOperator(SqlExpressionType exprType)
        {
            switch (exprType)
            {
                case SqlExpressionType.Add:
                    return "+";
                case SqlExpressionType.Subtract:
                    return "-";
                case SqlExpressionType.Multiply:
                    return "*";
                case SqlExpressionType.Divide:
                    return "/";
                case SqlExpressionType.Modulus:
                    return "%";
                case SqlExpressionType.Equal:
                    return "=";
                case SqlExpressionType.NotEqual:
                    return "<>";
                case SqlExpressionType.GreaterThan:
                    return ">";
                case SqlExpressionType.GreaterThanOrEqual:
                    return ">=";
                case SqlExpressionType.LessThan:
                    return "<";
                case SqlExpressionType.LessThanOrEqual:
                    return "<=";
                case SqlExpressionType.AndAlso:
                    return "and";
                case SqlExpressionType.OrElse:
                    return "or";
                //case SqlExpressionType.Like:
                //    return "like";
                default:
                    return "<opr>";
            }
        }
    }
}
