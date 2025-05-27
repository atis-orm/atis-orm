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
        SqlTableExpression CreateTable(string tableName, IReadOnlyList<TableColumn> tableColumns);
        SqlDerivedTableExpression ConvertSelectQueryToDeriveTable(SqlSelectExpression selectQuery);
        SqlDerivedTableExpression ConvertSelectQueryToUnwrappableDeriveTable(SqlSelectExpression selectQuery);
        SqlDerivedTableExpression ConvertSelectQueryToDataManipulationDerivedTable(SqlSelectExpression selectQuery);
        SqlSelectExpression CreateSelectQueryFromStandaloneSelect(SqlStandaloneSelectExpression standaloneSelect);
        SqlSelectExpression CreateSelectQuery(SqlExpression queryShape);
        SqlAliasExpression CreateAlias(string alias);
        SqlStringFunctionExpression CreateStringFunction(SqlStringFunction stringFunction, SqlExpression stringExpression, IReadOnlyList<SqlExpression> arguments);
        SqlFunctionCallExpression CreateFunctionCall(string functionName, SqlExpression[] arguments);
        SqlConditionalExpression CreateCondition(SqlExpression test, SqlExpression ifTrue, SqlExpression ifFalse);
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
        SqlExpression CreateJoinCondition(SqlExpression predicateLeft, SqlExpression predicateRight);
    }
}
