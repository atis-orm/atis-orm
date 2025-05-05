using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Abstractions
{
    public interface ISqlExpressionFactory
    {
        SqlBinaryExpression CreateBinary(SqlExpression left, SqlExpression right, SqlExpressionType sqlExpressionType);
        SqlLiteralExpression CreateLiteral(object value);
        SqlTableExpression CreateTable(string tableName, TableColumn[] tableColumns);
        SqlDerivedTableExpression ConvertSelectQueryToDeriveTable(SqlSelectExpression selectQuery);
        SqlDerivedTableExpression ConvertSelectQueryToUnwrappableDeriveTable(SqlSelectExpression selectQuery);
        SqlDerivedTableExpression ConvertSelectQueryToDataManipulationDerivedTable(SqlSelectExpression selectQuery);
        SqlSelectExpression CreateSelectQueryByTable(SqlTableExpression table);
        SqlSelectExpression CreateSelectQueryByFrom(SqlCompositeBindingExpression dataSources);
        SqlSelectExpression CreateSelectQueryFromStandaloneSelect(SelectColumn[] selectColumns);
        SqlSelectExpression CreateSelectQueryFromQuerySource(SqlQuerySourceExpression querySource);
        SqlAliasExpression CreateAlias(string alias);
        SqlStringFunctionExpression CreateStringFunction(SqlStringFunction stringFunction, SqlExpression stringExpression, SqlExpression[] arguments);
        SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments);
        SqlConditionalExpression CreateCondition(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse);
        SqlCompositeBindingExpression CreateCompositeBindingForSingleExpression(SqlExpression sqlExpression, ModelPath modelPath);
        SqlCompositeBindingExpression CreateCompositeBindingForMultipleExpressions(SqlExpressionBinding[] sqlExpressionBindings);
        SqlDefaultIfEmptyExpression CreateDefaultIfEmpty(SqlDerivedTableExpression derivedTable);
        SqlUnionQueryExpression CreateUnionQuery(UnionItem[] unionItems);
        SqlExistsExpression CreateExists(SqlDerivedTableExpression subQuery);
        SqlLikeExpression CreateLike(SqlExpression stringExpression, SqlExpression pattern);
        SqlLikeExpression CreateLikeStartsWith(SqlExpression stringExpression, SqlExpression pattern);
        SqlLikeExpression CreateLikeEndsWith(SqlExpression stringExpression, SqlExpression pattern);
        SqlDateAddExpression CreateDateAdd(SqlDatePart datePart, SqlExpression interval, SqlExpression dateExpression);
        SqlDateSubtractExpression CreateDateSubtract(SqlDatePart datePart, SqlExpression startDate, SqlExpression endDate);
        SqlCollectionExpression CreateCollection(IEnumerable<SqlExpression> sqlExpressions);
        SqlCastExpression CreateCast(SqlExpression expression, ISqlDataType sqlDataType);
        SqlDatePartExpression CreateDatePart(SqlDatePart datePart, SqlExpression dateExpr);
        SqlParameterExpression CreateParameter(object value, bool multipleValues);
        SqlInValuesExpression CreateInValuesExpression(SqlExpression expression, SqlExpression[] values);
        SqlNegateExpression CreateNegate(SqlExpression operand);
        SqlNotExpression CreateNot(SqlExpression sqlExpression);
        SqlUpdateExpression CreateUpdate(SqlDerivedTableExpression source, Guid dataSourceToUpdate, string[] columns, SqlExpression[] values);
        SqlDeleteExpression CreateDelete(SqlDerivedTableExpression source, Guid dataSourceAlias);
    }
}
