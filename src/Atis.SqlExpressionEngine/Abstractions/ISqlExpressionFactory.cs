using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Abstractions
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
        SqlParameterExpression CreateParameter(object value, bool multipleValues);
        SqlQueryExpression CreateCteQuery(Guid cteAlias, SqlQueryExpression anchorQuery);
        SqlQueryExpression CreateQueryFromDataSources(IEnumerable<SqlDataSourceExpression> dataSourceList);
        SqlQueryExpression CreateQueryFromDataSource(SqlDataSourceExpression sqlDataSourceExpression);
        SqlQueryExpression CreateQueryFromSelect(SqlExpression select);
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
        SqlInValuesExpression CreateInValuesExpression(SqlExpression expression, SqlExpression values);
        SqlQueryExpression CreateEmptySqlQuery();
        SqlQuerySourceExpression CreateSubQuery(SqlQueryExpression sqlQuery);
        SqlKeywordExpression CreateKeyword(string keyword);
        SqlNegateExpression CreateNegate(SqlExpression operand);
        SqlCastExpression CreateCast(SqlExpression expression, ISqlDataType sqlDataType);
        SqlDateAddExpression CreateDateAdd(SqlDatePart datePart, SqlExpression interval, SqlExpression dateExpression);
        SqlDateSubtractExpression CreateDateSubtract(SqlDatePart datePart, SqlExpression startDate, SqlExpression endDate);
        SqlDatePartExpression CreateDatePart(SqlDatePart datePart, SqlExpression dateExpr);
        SqlStringFunctionExpression CreateStringFunction(SqlStringFunction stringFunction, SqlExpression stringExpression, SqlExpression[] arguments);
        SqlLikeExpression CreateLike(SqlExpression stringExpression, SqlExpression pattern);
        SqlLikeExpression CreateLikeStartsWith(SqlExpression stringExpression, SqlExpression pattern);
        SqlLikeExpression CreateLikeEndsWith(SqlExpression stringExpression, SqlExpression pattern);
        SqlJoinExpression CreateNavigationJoin(SqlJoinType cross, SqlDataSourceExpression joinedSource, SqlExpression joinCondition, SqlExpression navigationParent, string navigationName);
    }
}
