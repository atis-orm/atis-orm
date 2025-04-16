using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine
{
    public static class ExtensionMethods
    {
        public static SqlQuerySourceExpression ConvertToTableIfPossible(this SqlQuerySourceExpression sqlExpression)
        {
            if (IsTableOnly(sqlExpression))
            {
                return ((SqlQueryExpression)sqlExpression).DataSources.First().QuerySource;
            }
            return sqlExpression;
        }

        public static bool IsTableOnly(this SqlQuerySourceExpression sqlQuerySource)
        {
            return sqlQuerySource is SqlQueryExpression sqlQuery && sqlQuery.IsTableOnly();
        }

        public static SqlColumnExpression[] GetProjections(this SqlExpression sqlExpression)
        {
            if (sqlExpression is SqlColumnExpression sqlColumnExpression)
            {
                return new[] { sqlColumnExpression };
            }
            else if (sqlExpression is SqlCollectionExpression sqlCollectionExpression)
            {
                return sqlCollectionExpression.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
            }
            else
                throw new InvalidOperationException($"Argument '{nameof(sqlExpression)}' must be of type '{nameof(SqlColumnExpression)}' or '{nameof(SqlCollectionExpression)}'.");
        }

        public static bool TryGetScalarColumn(this SqlExpression sqlExpression, out SqlColumnExpression sqlScalarColumn)
        {
            if (sqlExpression is SqlColumnExpression sqlColumnExpression && sqlColumnExpression.NodeType == SqlExpressionType.ScalarColumn)
            {
                sqlScalarColumn = sqlColumnExpression;
                return true;
            }
            sqlScalarColumn = null;
            return false;
        }

        public static string[] GetModelPath(this MemberExpression memberExpression)
        {
            var pathElements = new List<string>();
            do
            {
                pathElements.Add(memberExpression.Member.Name);
                memberExpression = memberExpression.Expression as MemberExpression;
            } while (memberExpression != null);
            return pathElements.Reverse<string>().ToArray();
        }

        public static bool TryCreateSubQueryDataSourceCopy(this SqlExpression sqlExpression, out SqlQueryExpression sqlQueryCopy)
        {
            if (sqlExpression is SqlDataSourceExpression ds && ds.NodeType == SqlExpressionType.SubQueryDataSource)
            {
                var otherDataSourceQuery = ds.QuerySource as SqlQueryExpression
                                            ??
                                            throw new InvalidOperationException($"'{ds.QuerySource}' is not a SqlQueryExpression");
                // other data source cannot be modified itself, it will always make a copy whenever used
                sqlQueryCopy = otherDataSourceQuery.CreateCopy();
                return true;
            }
            sqlQueryCopy = null;
            return false;
        }

        public static SqlDataSourceExpression[] GetColumnExpressionDataSources(SqlColumnExpression[] columnExpressions)
        {
            if (columnExpressions?.Length > 0 &&
                    columnExpressions.All(x => x.ColumnExpression is SqlDataSourceColumnExpression))
            {
                return columnExpressions.GroupBy(x => ((SqlDataSourceColumnExpression)x.ColumnExpression).DataSourceReference.Reference).Select(x => x.Key).ToArray();
            }
            return Array.Empty<SqlDataSourceExpression>();
        }
    }
}
