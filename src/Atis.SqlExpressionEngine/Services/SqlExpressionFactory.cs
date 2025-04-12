using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Services
{
    public class SqlExpressionFactory : ISqlExpressionFactory
    {
        public virtual SqlBinaryExpression CreateBinary(SqlExpression left, SqlExpression right, SqlExpressionType sqlExpressionType)
        {
            return new SqlBinaryExpression(left, right, sqlExpressionType);
        }

        public virtual SqlCollectionExpression CreateCollection(IEnumerable<SqlExpression> sqlExpressions)
        {
            return new SqlCollectionExpression(sqlExpressions);
        }

        public virtual SqlColumnExpression CreateColumn(SqlExpression sqlExpression, string columnAlias, ModelPath modelPath)
        {
            return new SqlColumnExpression(sqlExpression, columnAlias, modelPath, SqlExpressionType.Column);
        }

        public virtual SqlConditionalExpression CreateCondition(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse)
        {
            return new SqlConditionalExpression(test, ifTrue, ifFalse);
        }

        public virtual SqlCteReferenceExpression CreateCteReference(Guid cteAlias)
        {
            return new SqlCteReferenceExpression(cteAlias);
        }

        public virtual SqlDataSourceColumnExpression CreateDataSourceColumn(SqlDataSourceExpression dataSource, string columnAlias)
        {
            return new SqlDataSourceColumnExpression(dataSource, columnAlias);
        }

        public virtual SqlDataSourceExpression CreateDataSourceForCteReference(Guid dataSourceAlias, SqlCteReferenceExpression cteReference)
        {
            return new SqlDataSourceExpression(dataSourceAlias, cteReference);
        }

        public virtual SqlDataSourceExpression CreateDataSourceForNavigation(SqlQuerySourceExpression joinedSource, string navigationName)
        {
            return new SqlDataSourceExpression(joinedSource, modelPath: ModelPath.Empty, tag: navigationName);
        }

        public virtual SqlDataSourceExpression CreateDataSourceForQuerySource(SqlQuerySourceExpression sqlQuerySourceExpression)
        {
            return new SqlDataSourceExpression(sqlQuerySourceExpression);
        }

        public virtual SqlDataSourceExpression CreateDataSourceForSubQuery(Guid guid, SqlQuerySourceExpression querySource)
        {
            return new SqlDataSourceExpression(guid, querySource, modelPath: ModelPath.Empty, tag: null, nodeType: SqlExpressionType.SubQueryDataSource);
        }

        public virtual SqlDataSourceExpression CreateDataSourceForJoinedSource(Guid guid, SqlQuerySourceExpression querySource)
        {
            return new SqlDataSourceExpression(guid, querySource, modelPath: ModelPath.Empty, tag: null, nodeType: SqlExpressionType.DataSource);
        }

        public SqlDataSourceExpression CreateDataSourceForTable(SqlTableExpression sqlTableExpression)
        {
            return this.CreateDataSourceForQuerySource(sqlTableExpression);
        }

        public virtual SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments)
        {
            return new SqlFunctionCallExpression(functionName, arguments);
        }

        public virtual SqlLiteralExpression CreateLiteral(object value)
        {
            return new SqlLiteralExpression(value);
        }

        public virtual SqlOrderByExpression CreateOrderBy(SqlExpression orderByPart, bool ascending)
        {
            return new SqlOrderByExpression(orderByPart, ascending);
        }

        public virtual SqlDataSourceExpression CreateDataSourceCopy(SqlDataSourceExpression dataSource)
        {
            return new SqlDataSourceExpression(dataSource);
        }

        public virtual SqlDataSourceReferenceExpression CreateDataSourceReference(SqlDataSourceExpression dataSource)
        {
            return new SqlDataSourceReferenceExpression(dataSource);
        }

        public virtual SqlDataSourceReferenceExpression CreateQueryReference(SqlQueryExpression sqlQuery)
        {
            return new SqlDataSourceReferenceExpression(sqlQuery);
        }

        public virtual SqlDeleteExpression CreateDelete(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource)
        {
            return new SqlDeleteExpression(sqlQuery, selectedDataSource);
        }

        public virtual SqlExistsExpression CreateExists(SqlQueryExpression sqlQuery)
        {
            return new SqlExistsExpression(sqlQuery);
        }

        public SqlDataSourceExpression CreateFromSource(SqlQuerySourceExpression dataSource, ModelPath modelPath)
        {
            return new SqlDataSourceExpression(dataSourceAlias: Guid.NewGuid(), dataSource: dataSource, modelPath: modelPath, tag: null, nodeType: SqlExpressionType.FromSource);
        }

        public virtual SqlJoinExpression CreateJoin(SqlJoinType sqlJoinType, SqlDataSourceExpression joinedDataSource, SqlExpression joinPredicate)
        {
            return new SqlJoinExpression(sqlJoinType, joinedDataSource, joinPredicate);
        }

        public virtual SqlNotExpression CreateNot(SqlExpression sqlExpression)
        {
            return new SqlNotExpression(sqlExpression);
        }

        public virtual SqlParameterExpression CreateParameter(object value, bool multipleValues)
        {
            return new SqlParameterExpression(value, multipleValues);
        }

        public virtual SqlQueryExpression CreateCteQuery(Guid cteAlias, SqlQueryExpression subQuery)
        {
            return new SqlQueryExpression(cteAlias, subQuery, this);
        }

        public virtual SqlQueryExpression CreateQueryFromDataSources(IEnumerable<SqlDataSourceExpression> dataSourceList)
        {
            return new SqlQueryExpression(dataSourceList, this);
        }

        public virtual SqlQueryExpression CreateQueryFromDataSource(SqlDataSourceExpression dataSource)
        {
            return new SqlQueryExpression(dataSource, this);
        }

        public virtual SqlQueryExpression CreateQueryFromSelect(SqlExpression select)
        {
            return new SqlQueryExpression((SqlExpression)select, this);
        }

        public virtual SqlSelectedCollectionExpression CreateSelectedCollection(SqlExpression collectionSource, SqlExpression[] collection)
        {
            return new SqlSelectedCollectionExpression(collectionSource, collection);
        }

        public virtual SqlTableExpression CreateTable(string tableName, TableColumn[] tableColumns)
        {
            return new SqlTableExpression(tableName, tableColumns);
        }

        public virtual  SqlUpdateExpression CreateUpdate(SqlQueryExpression sqlQuery, SqlDataSourceExpression selectedDataSource, string[] columnNames, SqlExpression[] values)
        {
            return new SqlUpdateExpression(sqlQuery, selectedDataSource, columnNames, values);
        }

        public virtual SqlColumnExpression ChangeColumnAlias(SqlColumnExpression sqlColumnExpression, string newAlias)
        {
            return new SqlColumnExpression(sqlColumnExpression.ColumnExpression, newAlias, sqlColumnExpression.ModelPath, sqlColumnExpression.NodeType);
        }

        public virtual SqlColumnExpression CreateSubQueryColumn(SqlDataSourceColumnExpression columnExpression, string columnAlias, ModelPath modelPath)
        {
            return new SqlColumnExpression(columnExpression, columnAlias, modelPath, SqlExpressionType.SubQueryColumn);
        }

        public virtual SqlAliasExpression CreateAlias(string columnAlias)
        {
            return new SqlAliasExpression(columnAlias);
        }

        public virtual SqlUnionExpression CreateUnionAll(SqlQueryExpression query)
        {
            return new SqlUnionExpression(query, SqlExpressionType.UnionAll);
        }

        public virtual SqlUnionExpression CreateUnion(SqlQueryExpression query)
        {
            return new SqlUnionExpression(query, SqlExpressionType.Union);
        }

        public virtual SqlDataSourceExpression CreateDataSourceForCteQuery(Guid cteAlias, SqlQueryExpression cteSource)
        {
            return new SqlDataSourceExpression(cteAlias, dataSource: cteSource, modelPath: ModelPath.Empty, tag: null, nodeType: SqlExpressionType.CteDataSource);
        }

        public virtual SqlColumnExpression CreateScalarColumn(SqlExpression columnExpression, string columnAlias, ModelPath modelPath)
        {
            return new SqlColumnExpression(columnExpression, columnAlias, modelPath, SqlExpressionType.ScalarColumn);
        }

        public virtual SqlInValuesExpression CreateInValuesExpression(SqlExpression expression, SqlExpression values)
        {
            return new SqlInValuesExpression(expression, new[] { values });
        }

        public virtual SqlQueryExpression CreateEmptySqlQuery()
        {
            return new SqlQueryExpression(this);
        }

        public virtual SqlQuerySourceExpression CreateSubQuery(SqlQueryExpression sqlQuery)
        {
            return new SqlSubQueryExpression(sqlQuery);
        }

        public virtual SqlKeywordExpression CreateKeyword(string keyword)
        {
            return new SqlKeywordExpression(keyword);
        }

        public virtual SqlNegateExpression CreateNegate(SqlExpression operand)
        {
            return new SqlNegateExpression(operand);
        }

        public virtual SqlCastExpression CreateCast(SqlExpression expression, ISqlDataType sqlDataType)
        {
            return new SqlCastExpression(expression, sqlDataType);
        }

        public virtual SqlDateAddExpression CreateDateAdd(SqlDatePart datePart, SqlExpression interval, SqlExpression dateExpression)
        {
            return new SqlDateAddExpression(datePart, interval, dateExpression);
        }

        public virtual SqlDatePartExpression CreateDatePart(SqlDatePart datePart, SqlExpression dateExpression)
        {
            return new SqlDatePartExpression(datePart, dateExpression);
        }

        public virtual SqlStringFunctionExpression CreateStringFunction(SqlStringFunction stringFunction, SqlExpression stringExpression, SqlExpression[] arguments)
        {
            return new SqlStringFunctionExpression(stringFunction, stringExpression, arguments);
        }

        public virtual SqlLikeExpression CreateLike(SqlExpression stringExpression, SqlExpression pattern)
        {
            return new SqlLikeExpression(stringExpression, pattern, SqlExpressionType.Like);
        }

        public virtual SqlLikeExpression CreateLikeStartsWith(SqlExpression stringExpression, SqlExpression pattern)
        {
            return new SqlLikeExpression(stringExpression, pattern, SqlExpressionType.LikeStartsWith);
        }

        public virtual SqlLikeExpression CreateLikeEndsWith(SqlExpression stringExpression, SqlExpression pattern)
        {
            return new SqlLikeExpression(stringExpression, pattern, SqlExpressionType.LikeEndsWith);
        }
    }
}
