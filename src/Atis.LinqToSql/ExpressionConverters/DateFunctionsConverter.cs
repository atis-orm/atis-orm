using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
{
    public class DateFunctionsConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        private static readonly string[] SupportedMethods = new[]
        {
            nameof(DateTime.AddDays),
            nameof(DateTime.AddMonths),
            nameof(DateTime.AddYears)
        };

        public DateFunctionsConverterFactory(IConversionContext context) : base(context) { }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCall &&
                methodCall.Method.DeclaringType == typeof(DateTime) &&
                SupportedMethods.Contains(methodCall.Method.Name))
            {
                converter = new DateFunctionsConverter(this.Context, methodCall, converterStack);
                return true;
            }

            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts supported DateTime instance methods like AddDays, AddMonths, AddYears into SQL DATEADD function calls.
    ///     </para>
    /// </summary>
    public class DateFunctionsConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        public DateFunctionsConverter(
            IConversionContext context,
            MethodCallExpression expression,
            ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var methodName = this.Expression.Method.Name;
            var dateExpr = convertedChildren[0];      // e.g., DateTime instance
            var argExpr = convertedChildren[1];       // e.g., number of days/months/years

            if (methodName == nameof(DateTime.AddDays))
                return CreateDateAdd("day", dateExpr, argExpr);

            if (methodName == nameof(DateTime.AddMonths))
                return CreateDateAdd("month", dateExpr, argExpr);

            if (methodName == nameof(DateTime.AddYears))
                return CreateDateAdd("year", dateExpr, argExpr);

            throw new NotSupportedException("Unsupported DateTime method: " + methodName);
        }

        private SqlFunctionCallExpression CreateDateAdd(string part, SqlExpression dateExpr, SqlExpression amountExpr)
        {
            return this.SqlFactory.CreateFunctionCall("dateadd",
                new[] { this.SqlFactory.CreateKeyword(part), amountExpr, dateExpr });
        }
    }

}
