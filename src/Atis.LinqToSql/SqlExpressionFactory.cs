using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql
{
    public class SqlExpressionFactory : ISqlExpressionFactory
    {
        public SqlBinaryExpression CreateBinary(SqlExpression left, SqlExpression right, SqlExpressionType sqlExpressionType)
        {
            return new SqlBinaryExpression(left, right, sqlExpressionType);
        }

        public SqlCollectionExpression CreateCollection(IEnumerable<SqlExpression> sqlExpressions)
        {
            return new SqlCollectionExpression(sqlExpressions);
        }

        public SqlColumnExpression CreateColumn(SqlExpression sqlExpression, string columnAlias, ModelPath modelPath)
        {
            return new SqlColumnExpression(sqlExpression, columnAlias, modelPath, SqlExpressionType.Column);
        }

        public SqlConditionalExpression CreateCondition(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse)
        {
            return new SqlConditionalExpression(test, ifTrue, ifFalse);
        }

        public SqlCteReferenceExpression CreateCteReference(Guid cteAlias)
        {
            return new SqlCteReferenceExpression(cteAlias);
        }

        public SqlDataSourceColumnExpression CreateDataSourceColumn(SqlDataSourceExpression dataSource, string columnAlias)
        {
            return new SqlDataSourceColumnExpression(dataSource, columnAlias);
        }

        public SqlDataSourceExpression CreateDataSourceForCteReference(Guid dataSourceAlias, SqlCteReferenceExpression cteReference)
        {
            return new SqlDataSourceExpression(dataSourceAlias, cteReference);
        }

        public SqlDataSourceExpression CreateDataSourceForNavigation(SqlQuerySourceExpression joinedSource, string navigationName)
        {
            return new SqlDataSourceExpression(joinedSource, modelPath: ModelPath.Empty, tag: navigationName);
        }

        public SqlDataSourceExpression CreateDataSourceForQuerySource(SqlQuerySourceExpression sqlQuerySourceExpression)
        {
            return new SqlDataSourceExpression(sqlQuerySourceExpression);
        }

        public SqlDataSourceExpression CreateDataSourceForSubQuery(Guid guid, SqlQuerySourceExpression querySource)
        {
            return new SqlDataSourceExpression(guid, querySource, modelPath: ModelPath.Empty, tag: null, nodeType: SqlExpressionType.SubQueryDataSource);
        }

        public SqlDataSourceExpression CreateDataSourceForJoinedSource(Guid guid, SqlQuerySourceExpression querySource)
        {
            return new SqlDataSourceExpression(guid, querySource, modelPath: ModelPath.Empty, tag: null, nodeType: SqlExpressionType.DataSource);
        }

        public SqlDataSourceExpression CreateDataSourceForTable(SqlTableExpression sqlTableExpression)
        {
            return this.CreateDataSourceForQuerySource(sqlTableExpression);
        }

        public SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments)
        {
            return new SqlFunctionCallExpression(functionName, arguments);
        }

        public SqlLiteralExpression CreateLiteral(object value)
        {
            return new SqlLiteralExpression(value);
        }

        public SqlOrderByExpression CreateOrderBy(SqlExpression orderByPart, bool ascending)
        {
            return new SqlOrderByExpression(orderByPart, ascending);
        }

        public SqlDataSourceExpression CreateDataSourceCopy(SqlDataSourceExpression dataSource)
        {
            return new SqlDataSourceExpression(dataSource);
        }

        public SqlDataSourceReferenceExpression CreateDataSourceReference(SqlDataSourceExpression dataSource)
        {
            return new SqlDataSourceReferenceExpression(dataSource);
        }

        public SqlDataSourceReferenceExpression CreateQueryReference(SqlQueryExpression sqlQuery)
        {
            return new SqlDataSourceReferenceExpression(sqlQuery);
        }

        public SqlDeleteExpression CreateDelete(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource)
        {
            return new SqlDeleteExpression(sqlQuery, selectedDataSource);
        }

        public SqlExistsExpression CreateExists(SqlQueryExpression sqlQuery)
        {
            return new SqlExistsExpression(sqlQuery);
        }

        public SqlDataSourceExpression CreateFromSource(SqlQuerySourceExpression dataSource, ModelPath modelPath)
        {
            return new SqlDataSourceExpression(dataSourceAlias: Guid.NewGuid(), dataSource: dataSource, modelPath: modelPath, tag: null, nodeType: SqlExpressionType.FromSource);
        }

        public SqlJoinExpression CreateJoin(SqlJoinType sqlJoinType, SqlDataSourceExpression joinedDataSource, SqlExpression joinPredicate)
        {
            return new SqlJoinExpression(sqlJoinType, joinedDataSource, joinPredicate);
        }

        public SqlJoinExpression CreateCrossApplyOrOuterApplyJoin(SqlJoinType sqlJoinType, SqlDataSourceExpression newDataSource)
        {
            return this.CreateJoin(sqlJoinType, newDataSource, joinPredicate: null);
        }

        public SqlNotExpression CreateNot(SqlExpression sqlExpression)
        {
            return new SqlNotExpression(sqlExpression);
        }

        public SqlParameterExpression CreateParameter(object value)
        {
            return new SqlParameterExpression(value);
        }

        public SqlQueryExpression CreateCteQuery(Guid cteAlias, SqlQueryExpression anchorQuery)
        {
            return new SqlQueryExpression(cteAlias, anchorQuery);
        }

        public SqlQueryExpression CreateQueryFromDataSources(IEnumerable<SqlDataSourceExpression> dataSourceList)
        {
            return new SqlQueryExpression(dataSourceList);
        }

        public SqlQueryExpression CreateQueryFromDataSource(SqlDataSourceExpression dataSource)
        {
            return new SqlQueryExpression(dataSource);
        }

        public SqlSelectedCollectionExpression CreateSelectedCollection(SqlExpression collectionSource, SqlExpression[] collection)
        {
            return new SqlSelectedCollectionExpression(collectionSource, collection);
        }

        public SqlTableExpression CreateTable(string tableName, TableColumn[] tableColumns)
        {
            return new SqlTableExpression(tableName, tableColumns);
        }

        public SqlUpdateExpression CreateUpdate(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource, string[] columnNames, SqlExpression[] values)
        {
            return new SqlUpdateExpression(sqlQuery, selectedDataSource, columnNames, values);
        }
    }
}
