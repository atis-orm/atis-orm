using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql.Abstractions
{
    public interface ISqlExpressionFactory
    {
        SqlBinaryExpression CreateBinary(SqlExpression left, SqlExpression right, SqlExpressionType sqlExpressionType);
        SqlCollectionExpression CreateCollection(IEnumerable<SqlExpression> sqlExpressions);
        SqlColumnExpression CreateColumn(SqlExpression sqlExpression, string columnAlias, ModelPath modelPath);
        SqlConditionalExpression CreateCondition(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse);
        SqlCteReferenceExpression CreateCteReference(Guid cteAlias);
        SqlDataSourceColumnExpression CreateDataSourceColumn(SqlDataSourceExpression dataSource, string columnAlias);
        SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments);
        SqlLiteralExpression CreateLiteral(object value);
        SqlDataSourceExpression CreateDataSourceForTable(SqlTableExpression sqlTableExpression);
        SqlDataSourceExpression CreateDataSourceForQuerySource(SqlQuerySourceExpression sqlQuerySourceExpression);
        SqlOrderByExpression CreateOrderBy(SqlExpression orderBy, bool ascending);
        SqlDataSourceExpression CreateDataSourceForCteReference(Guid dataSourceAlias, SqlCteReferenceExpression cteReference);
        SqlDataSourceExpression CreateDataSourceForNavigation(SqlQuerySourceExpression joinedSource, string navigationName);
        SqlDataSourceExpression CreateDataSourceForSubQuery(Guid guid, SqlQuerySourceExpression querySource);
        SqlDataSourceExpression CreateDataSourceForJoinedSource(Guid guid, SqlQuerySourceExpression querySource);
        SqlDataSourceExpression CreateDataSourceCopy(SqlDataSourceExpression cteDataSource);
        SqlDataSourceReferenceExpression CreateDataSourceReference(SqlDataSourceExpression dataSource);
        SqlDataSourceReferenceExpression CreateQueryReference(SqlQueryExpression sqlQuery);
        SqlDeleteExpression CreateDelete(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource);
        SqlExistsExpression CreateExists(SqlQueryExpression sqlQuery);
        SqlDataSourceExpression CreateFromSource(SqlQuerySourceExpression dataSource, ModelPath modelPath);
        SqlJoinExpression CreateJoin(SqlJoinType sqlJoinType, SqlDataSourceExpression joinedDataSource, SqlExpression joinPredicate);
        SqlNotExpression CreateNot(SqlExpression sqlExpression);
        SqlParameterExpression CreateParameter(object value);
        SqlQueryExpression CreateCteQuery(Guid cteAlias, SqlQueryExpression anchorQuery);
        SqlQueryExpression CreateQueryFromDataSources(IEnumerable<SqlDataSourceExpression> dataSourceList);
        SqlQueryExpression CreateQueryFromDataSource(SqlDataSourceExpression sqlDataSourceExpression);
        SqlSelectedCollectionExpression CreateSelectedCollection(SqlExpression collectionSource, SqlExpression[] collection);
        SqlTableExpression CreateTable(string tableName, TableColumn[] tableColumns);
        SqlUpdateExpression CreateUpdate(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource, string[] columnNames, SqlExpression[] values);
        SqlColumnExpression ChangeColumnAlias(SqlColumnExpression sqlColumnExpression, string alias);
        SqlColumnExpression CreateSubQueryColumn(SqlDataSourceColumnExpression dataSourceColumn, string columnAlias, ModelPath modelPath);
        SqlAliasExpression CreateAlias(string columnAlias);
        SqlUnionExpression CreateUnionAll(SqlQueryExpression sqlQuery);
        SqlUnionExpression CreateUnion(SqlQueryExpression sqlQuery);
        SqlDataSourceExpression CreateDataSourceForCteQuery(Guid cteAlias, SqlQueryExpression cteSource);
        SqlColumnExpression CreateScalarColumn(SqlExpression columnExpression, string columnAlias, ModelPath modelPath);
    }
}
