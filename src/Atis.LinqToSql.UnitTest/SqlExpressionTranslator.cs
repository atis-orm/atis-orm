using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
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

        private string GetExpressionId(Guid expressionId)
        {
            return string.Empty;
            //if (!this.expressionIdCache.TryGetValue(expressionId, out var id))
            //{
            //    id = this.expressionIdCache.Count + 1;
            //    this.expressionIdCache.Add(expressionId, id);
            //}
            //return $" /*{id}*/ ";
        }

        public string Translate(SqlExpression sqlExpression)
        {
            if (sqlExpression is SqlBinaryExpression sqlBinaryExpression)
            {
                return this.TranslateSqlBinaryExpression(sqlBinaryExpression);
            }
            else if (sqlExpression is SqlColumnExpression sqlColumnExpression)
            {
                return this.TranslateSqlColumnExpression(sqlColumnExpression);
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
            else if (sqlExpression is SqlDataSourceExpression sqlDataSourceExpression)
            {
                return this.TranslateSqlDataSourceExpression(sqlDataSourceExpression);
            }
            else if (sqlExpression is SqlFunctionCallExpression sqlFunctionCallExpression)
            {
                return this.TranslateSqlFunctionCallExpression(sqlFunctionCallExpression);
            }
            else if (sqlExpression is SqlJoinExpression sqlJoinExpression)
            {
                return this.TranslateSqlJoinExpression(sqlJoinExpression);
            }
            else if (sqlExpression is SqlOrderByExpression sqlOrderByExpression)
            {
                return this.TranslateSqlOrderByExpression(sqlOrderByExpression);
            }
            else if (sqlExpression is SqlParameterExpression sqlParameterExpression)
            {
                return this.TranslateSqlParameterExpression(sqlParameterExpression);
            }
            else if (sqlExpression is SqlQueryExpression sqlQueryExpression)
            {
                return this.TranslateSqlQueryExpression(sqlQueryExpression);
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
                return $"not {this.Translate(sqlNotExpression.Operand)}";
            }
            else
            {
                throw new NotSupportedException($"SqlExpression type '{sqlExpression?.GetType().Name}' is not supported.");
            }
        }

        private string TranslateSqlUpdateExpression(SqlUpdateExpression sqlUpdateExpression)
        {
            var updateColumns = sqlUpdateExpression.Columns.Zip(sqlUpdateExpression.Values, (c, v) => $"{c} = {this.Translate(v)}");
            var query = $"update {this.GetSimpleAlias(sqlUpdateExpression.UpdatingDataSource.DataSourceAlias, sqlUpdateExpression.UpdatingDataSource.Tag)}\r\n\tset {string.Join(",\r\n\t\t", updateColumns)}\r\n{this.Translate(sqlUpdateExpression.SqlQuery)}";
            return query;
        }

        private string TranslateSqlDeleteExpression(SqlDeleteExpression sqlDeleteExpression)
        {
            var query = $"delete {this.GetSimpleAlias(sqlDeleteExpression.DeletingDataSource.DataSourceAlias, sqlDeleteExpression.DeletingDataSource.Tag)}\r\n{this.Translate(sqlDeleteExpression.SqlQuery)}";
            return query;
        }

        private string TranslateSqlConditionalExpression(SqlConditionalExpression sqlConditionExpression)
        {
            var test = this.Translate(sqlConditionExpression.Test);
            var ifTrue = this.Translate(sqlConditionExpression.IfTrue);
            var ifFalse = this.Translate(sqlConditionExpression.IfFalse);
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

        private readonly HashSet<SqlQueryExpression> sqlQueryExpressions = new HashSet<SqlQueryExpression>();

        //public string TranslateSqlCteExpression(SqlCteExpression sqlCteExpression)
        //{
        //    //var cteQuery = this.Translate(sqlCteExpression.CteQuery);
        //    //return $"with {sqlCteExpression.CteAlias} as (\r\n\t{cteQuery.Replace("\r\n", "\r\n\t")}\r\n)";
        //}


        // Helper method to process the WhereClause and group OR conditions
        private string ProcessWhereOrClauses(IEnumerable<FilterPredicate> predicates)
        {
            StringBuilder result = new StringBuilder();
            bool isOrGroupOpen = false;
            var predicateArray = predicates.ToArray();

            for(var i = 0; i < predicateArray.Length; i++)
            {
                var predicate = predicateArray[i];
                var nextEntry = i < predicateArray.Length - 1 ? predicateArray[i + 1] : null;
                if (i > 0)
                {
                    result.Append(predicate.UseOrOperator ? " or " : " and ");
                }
                if (!isOrGroupOpen)
                {
                    if (nextEntry?.UseOrOperator == true)
                    {
                        result.Append("(");
                        isOrGroupOpen = true;
                    }
                }
                var translatedPredicate = this.Translate(predicate.Predicate);
                result.Append(translatedPredicate);
                if (isOrGroupOpen)
                {
                    if ((nextEntry?.UseOrOperator ?? false) == false)
                    {
                        result.Append(")");
                        isOrGroupOpen = false;
                    }
                }
            }

            return result.ToString();
        }

        private string TranslateSqlQueryExpression(SqlQueryExpression sqlQueryExpression)
        {
            if (this.sqlQueryExpressions.Contains(sqlQueryExpression))
            {
                throw new InvalidOperationException($"Sql Query already exists");
            }
            //if (sqlQueryExpression.IsTableOnly())
            //{
            //    return this.Translate(sqlQueryExpression.DataSources.First().DataSource);
            //}
            this.sqlQueryExpressions.Add(sqlQueryExpression);
            // WhereClause conversion with dynamic AND/OR based on UseOrOperator, grouping OR conditions in parentheses

            string? initialDataSource = null;
            string[] dataSourceToString = Array.Empty<string>();
            string? fromString = null;
            if (sqlQueryExpression.IsCte)
            {
                if (sqlQueryExpression.CteDataSources.Count > 0)
                {
                    initialDataSource = this.Translate(sqlQueryExpression.InitialDataSource);
                    dataSourceToString = sqlQueryExpression.CteDataSources.Select(x => $"{this.GetSimpleAlias(x.DataSourceAlias, "cte")} as \r\n{this.Translate(x.DataSource).Replace("\r\n", "\t\r\n")}").ToArray();
                    fromString = initialDataSource;
                }
                else
                {
                    dataSourceToString = sqlQueryExpression.DataSources.Take(1).Select(x => $"{this.GetSimpleAlias(x.DataSourceAlias, x.Tag)} as \r\n{this.Translate(x.DataSource).Replace("\r\n", "\t\r\n")}").ToArray();
                    fromString = this.GetSimpleAlias(sqlQueryExpression.InitialDataSource.DataSourceAlias, sqlQueryExpression.InitialDataSource.Tag);
                }
            }
            else
            {
                initialDataSource = this.Translate(sqlQueryExpression.InitialDataSource);
            }
                

            string joins;
            if (sqlQueryExpression.IsCte)
            {
                if (sqlQueryExpression.CteDataSources.Count > 0)
                {
                    joins = sqlQueryExpression.Joins.Count > 0 ? "\r\n\t" + string.Join("\r\n\t", sqlQueryExpression.Joins.Select(this.Translate).Select(x => x.Replace("\r\n", "\r\n\t"))) : string.Empty;
                }
                else
                {
                    joins = sqlQueryExpression.Joins.Count > 0 ? "\r\n\t" + string.Join("\r\n\t", sqlQueryExpression.Joins.Select(this.TranslateCteJoin).Select(x => x.Replace("\r\n", "\r\n\t"))) : string.Empty;
                }
            }
            else
            {
                joins = sqlQueryExpression.Joins.Count > 0 ? "\r\n\t" + string.Join("\r\n\t", sqlQueryExpression.Joins.Select(this.Translate).Select(x => x.Replace("\r\n", "\r\n\t"))) : string.Empty;
            }

            var wherePart = sqlQueryExpression.WhereClause.Count > 0
                ? $"\r\nwhere\t{ProcessWhereOrClauses(sqlQueryExpression.WhereClause)}"
                : string.Empty;

            // HavingClause conversion with dynamic AND/OR based on UseOrOperator, with proper spacing for operators
            var havingPart = sqlQueryExpression.HavingClause.Count > 0
                ? $"\r\nhaving\t{ProcessWhereOrClauses(sqlQueryExpression.HavingClause)}"
                : string.Empty;


            //var joinsMissing = sqlQueryExpression.DataSources.Skip(1)
            //                        .Where(x => !sqlQueryExpression.Joins.Any(y => y.JoinedSource == x))
            //                        .Select(x => new SqlJoinExpression(SqlJoinType.Cross, x, null))
            //                        .ToList();
            //var finalList = sqlQueryExpression.Joins.Concat(joinsMissing).ToArray();
            // sort finaList using the order by sqlQueryExpression.DataSources
            //var dataSourcesWithIndex = sqlQueryExpression.DataSources.Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i);
            //finalList = finalList.OrderBy(x => dataSourcesWithIndex[x.JoinedSource]).ToArray();
            var topPart = sqlQueryExpression.Top != null ? $"\ttop ({this.Translate(sqlQueryExpression.Top)})" : string.Empty;
            string groupByPart = string.Empty;
            if (sqlQueryExpression.GroupBy != null)
            {
                if (sqlQueryExpression.GroupBy is SqlCollectionExpression sqlCollection)
                {
                    var colExpressions = sqlCollection.SqlExpressions.Select(x => x is SqlColumnExpression colExpr ? colExpr.ColumnExpression : x).ToArray();
                    groupByPart = $"\r\ngroup by {string.Join(", ", colExpressions.Select(this.Translate))}";
                }
                else
                {
                    groupByPart = $"\r\ngroup by {this.Translate(sqlQueryExpression.GroupBy)}";
                }
            }
            var orderByPart = sqlQueryExpression.OrderBy.Count > 0 ? $"\r\norder by {string.Join(", ", sqlQueryExpression.OrderBy.Select(this.Translate))}" : string.Empty;
            string? projectionPart = null;
            if (sqlQueryExpression.Projection != null)
            {
                projectionPart = this.Translate(sqlQueryExpression.Projection);
            }
            var pagingPart = string.Empty;
            if (sqlQueryExpression.RowsPerPage != null && sqlQueryExpression.RowOffset != null)
            {
                var rowOffset = this.Translate(new SqlLiteralExpression(sqlQueryExpression.RowOffset));
                var rowsPerPage = this.Translate(new SqlLiteralExpression(sqlQueryExpression.RowsPerPage));
                pagingPart = $"\r\noffset {rowOffset} rows fetch next {rowsPerPage} rows only";
            }
            var unions = string.Empty;
            if (sqlQueryExpression.Unions.Count > 0)
            {
                unions = "\r\n" + string.Join("\r\n", sqlQueryExpression.Unions.Select(x => $"{(x.NodeType == SqlExpressionType.UnionAll ? "union all" : "union")}\r\n{this.RemoveParenthesis(this.Translate(x.Query).Replace("\r\n\t", "\r\n"))}"));
            }
            var distinct = sqlQueryExpression.IsDistinct ? "\tdistinct" : string.Empty;
            string query;
            if (sqlQueryExpression.IsCte)
            {
                //string[] dataSourceToString;
                //string fromString;
                //if (sqlQueryExpression.CteDataSources.Count > 0)
                //{
                //    dataSourceToString = sqlQueryExpression.CteDataSources.Select(x => $"{this.GetSimpleAlias(x.DataSourceAlias, x.Tag)} as \r\n{this.Translate(x.DataSource).Replace("\r\n", "\t\r\n")}").ToArray();
                //    fromString = initialDataSource;
                //}
                //else
                //{
                //    dataSourceToString = sqlQueryExpression.DataSources.Take(1).Select(x => $"{this.GetSimpleAlias(x.DataSourceAlias, x.Tag)} as \r\n{this.Translate(x.DataSource).Replace("\r\n", "\t\r\n")}").ToArray();
                //    fromString = this.GetSimpleAlias(sqlQueryExpression.InitialDataSource.DataSourceAlias, sqlQueryExpression.InitialDataSource.Tag);
                //}
                query = $"{this.GetExpressionId(sqlQueryExpression.Id)}with {string.Join(", ", dataSourceToString)}\r\nselect{distinct}{topPart}\t{projectionPart}\r\nfrom\t{fromString}{joins}{wherePart}{groupByPart}{havingPart}{orderByPart}{pagingPart}{unions}";
            }
            else
            {
                string selectPart = string.Empty;
                if (projectionPart != null)
                    selectPart = $"select{distinct}{topPart}\t{projectionPart}\r\n";
                query = $"{this.GetExpressionId(sqlQueryExpression.Id)}{selectPart}from\t{initialDataSource}{joins}{wherePart}{groupByPart}{havingPart}{orderByPart}{pagingPart}{unions}";
                if (projectionPart != null)
                    query = $"(\r\n\t{query.Replace("\r\n", "\r\n\t")}\r\n)";
            }
            this.sqlQueryExpressions.Remove(sqlQueryExpression);
            return query;
        }

        private string TranslateCteJoin(SqlJoinExpression join)
        {
            var condition = join.JoinCondition != null ? $" on {this.Translate(join.JoinCondition)}" : "";
            return $"{JoinTypeToString(join.JoinType)} {this.GetSimpleAlias(join.JoinedSource.DataSourceAlias, join.JoinedSource.Tag)}{condition}";
        }


        int paramCount = 0;
        private string TranslateSqlParameterExpression(SqlParameterExpression sqlParameterExpression)
        {
            //return $"@p{paramCount++}";
            return SqlParameterExpression.ConvertObjectToString(sqlParameterExpression.Value);
        }

        private string TranslateSqlOrderByExpression(SqlOrderByExpression sqlOrderByExpression)
        {
            return $"{this.Translate(sqlOrderByExpression.Expression)} {(sqlOrderByExpression.Ascending ? "asc" : "desc")}";
        }

        private string TranslateSqlJoinExpression(SqlJoinExpression sqlJoinExpression)
        {
            var dataSource = this.Translate(sqlJoinExpression.JoinedSource);
            var condition = sqlJoinExpression.JoinCondition != null ? $" on {this.Translate(sqlJoinExpression.JoinCondition)}" : "";
            string joinType = JoinTypeToString(sqlJoinExpression.JoinType);
            return $"{joinType} {dataSource}{condition}";
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
            var arguments = sqlFunctionCallExpression.Arguments != null ? string.Join(", ", sqlFunctionCallExpression.Arguments.Select(this.Translate)) : string.Empty;
            if (sqlFunctionCallExpression.FunctionName == "Count")
            {
                arguments = string.IsNullOrWhiteSpace(arguments) ? "1" : arguments;
            }
            return $"{sqlFunctionCallExpression.FunctionName}({arguments})";
        }

        private string TranslateSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
        {
            var dataSourceTranslated = this.Translate(sqlDataSourceExpression.DataSource);
            //if (sqlDataSourceExpression.DataSource is SqlQueryExpression)
            //{
            //    dataSourceTranslated = $"(\r\n\t{dataSourceTranslated.Replace("\r\n", "\r\n\t")}\r\n)";
            //}
            return $"{dataSourceTranslated} as {this.GetSimpleAlias(sqlDataSourceExpression.DataSourceAlias, sqlDataSourceExpression.Tag)}{this.GetExpressionId(sqlDataSourceExpression.Id)}";
        }

        private string TranslateSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
        {
            return $"{this.GetSimpleAlias(sqlDataSourceColumnExpression.DataSource.DataSourceAlias)}.{sqlDataSourceColumnExpression.ColumnName}";
        }

        private string TranslateSqlExistsExpression(SqlExistsExpression sqlExistsExpression)
        {
            var sqlQuery = this.Translate(sqlExistsExpression.SqlQuery);
            return $"exists{sqlQuery}";
        }

        private string TranslateSqlCollectionExpression(SqlCollectionExpression sqlCollectionExpression)
        {
            return string.Join(", ", sqlCollectionExpression.SqlExpressions.Select(this.Translate));
        }

        private string TranslateSqlLiteralExpression(SqlLiteralExpression sqlLiteralExpression)
        {
            return SqlParameterExpression.ConvertObjectToString(sqlLiteralExpression.LiteralValue);
        }

        private string TranslateSqlColumnExpression(SqlColumnExpression sqlColumnExpression)
        {
            return $"{this.Translate(sqlColumnExpression.ColumnExpression)} as {sqlColumnExpression.ColumnAlias}";
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

                if (sqlBinaryExpression.NodeType == SqlExpressionType.Equal && IsNull(sqlBinaryExpression.Right))
                    return $"{this.Translate(sqlBinaryExpression.Left)} is null";
                else
                    return $"({this.Translate(sqlBinaryExpression.Left)} {GetOperator(sqlBinaryExpression.NodeType)} {this.Translate(sqlBinaryExpression.Right)})";
            }
            else
            {
                return $"isnull({this.Translate(sqlBinaryExpression.Left)}, {this.Translate(sqlBinaryExpression.Right)})";
            }
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
                case SqlExpressionType.Like:
                    return "like";
                default:
                    return "<opr>";
            }
        }
    }
}
