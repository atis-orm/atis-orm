using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql
{
    public static class ExtensionMethods
    {
        public static SqlQuerySourceExpression ConvertToTableIfPossible(this SqlQuerySourceExpression sqlExpression)
        {
            if (IsTableOnly(sqlExpression))
            {
                return ((SqlQueryExpression)sqlExpression).DataSources.First().DataSource;
            }
            return sqlExpression;
        }

        public static bool IsTableOnly(this SqlQuerySourceExpression sqlQuerySource)
        {
            return sqlQuerySource is SqlQueryExpression sqlQuery && sqlQuery.IsTableOnly();
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
    }
}
