using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql.Internal
{
    public class ProjectionCreator
    {
        public IEnumerable<SqlColumnExpression> Create(SqlCollectionExpression sqlCollection)
        {
            return this.Create(new [] { sqlCollection }, new string[] { null });
        }

        public IEnumerable<SqlColumnExpression> Create(SqlExpression[] sqlExpressions, string[] columnAliases)
        {
            var myColExprList = new List<SqlColumnExpression>();
            for (var i = 0; i < sqlExpressions.Length; i++)
            {
                var argument = sqlExpressions[i];
                if (argument is SqlCollectionExpression sqlCollection)
                {
                    if (sqlCollection.SqlExpressions is null)
                        throw new InvalidOperationException($"SqlExpressions of the SqlCollectionExpression at index {i} is null.");
                    var subArgs = sqlCollection.SqlExpressions.ToArray();
                    var newSubArgs = new List<SqlColumnExpression>();
                    for (var j = 0; j < subArgs.Length; j++)
                    {
                        var subArg = subArgs[j];
                        ModelPath subArgModelPath;
                        SqlExpression subArgColExpr;
                        string subArgColAlias;
                        if (subArg is SqlDataSourceReferenceExpression sqlDsRef && sqlDsRef.DataSource is SqlDataSourceExpression sqlDs)
                        {
                            subArgModelPath = sqlDs.ModelPath;
                            subArgColExpr = sqlDsRef;
                            subArgColAlias = null;
                        }
                        else if (subArg is SqlColumnExpression sqlCol)
                        {
                            subArgModelPath = sqlCol.ModelPath;
                            subArgColExpr = sqlCol.ColumnExpression;
                            subArgColAlias = sqlCol.ColumnAlias;
                        }
                        else
                            throw new InvalidOperationException($"subArg is neither SqlDataSourceReferenceExpression nor SqlColumnExpression");
                        var newColExpr = new SqlColumnExpression(subArgColExpr, subArgColAlias, subArgModelPath);
                        newSubArgs.Add(newColExpr);
                    }
                    argument = new SqlCollectionExpression(newSubArgs);
                }
                var myColExpr = new SqlColumnExpression(argument, columnAliases[i], new ModelPath(columnAliases[i]));
                myColExprList.Add(myColExpr);
            }

            var columnExpressionList = new List<SqlColumnExpression>();
            // this method will iterate through myColumnExpressions and add them to columnExpressionList
            // also it checks each entry in myColumnExpressions if it is a SqlCollectionExpression and if so,
            // it will add its SqlColumnExpressions to columnExpressionList
            AddColumnExpressions(myColExprList, new ModelPath(path: null), columnExpressionList);
            return columnExpressionList;
        }

        private void AddColumnExpressions(IEnumerable<SqlColumnExpression> sqlColumnExpressions, ModelPath parentMap, List<SqlColumnExpression> columnExpressions)
        {
            var i = 0;
            foreach (var sqlColumnExpression in sqlColumnExpressions)
            {
                var argument = sqlColumnExpression.ColumnExpression
                                ??
                                throw new InvalidOperationException($"Argument at index {i} is null.");
                var argumentModelPath = sqlColumnExpression.ModelPath;
                if (argument is SqlCollectionExpression sqlCollection)
                {
                    if (sqlCollection.SqlExpressions is null)
                        throw new InvalidOperationException($"SqlExpressions of the SqlCollectionExpression at index {i} is null.");
                    var subArgs = sqlCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();
                    AddColumnExpressions(subArgs, argumentModelPath, columnExpressions);
                }
                else
                {
                    var newCol = new SqlColumnExpression(sqlColumnExpression.ColumnExpression, sqlColumnExpression.ColumnAlias, parentMap.Append(sqlColumnExpression.ModelPath));
                    columnExpressions.Add(newCol);
                }
                i++;
            }
        }
    }
}
