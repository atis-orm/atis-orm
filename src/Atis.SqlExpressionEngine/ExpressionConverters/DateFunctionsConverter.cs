using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class DateFunctionsConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        private static readonly string[] SupportedMethods = new[]
        {
            nameof(DateTime.AddYears),
            nameof(DateTime.AddMonths),
            nameof(DateTime.AddDays),
            nameof(DateTime.AddHours),
            nameof(DateTime.AddMinutes),
            nameof(DateTime.AddSeconds),
            nameof(DateTime.AddMilliseconds),
            nameof(DateTime.AddTicks),
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
    public class DateFunctionsConverter : LinqToNonSqlQueryConverterBase<MethodCallExpression>
    {
        public DateFunctionsConverter(
            IConversionContext context,
            MethodCallExpression expression,
            ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var methodName = this.Expression.Method.Name;
            var dateExpr = convertedChildren[0];      // e.g., DateTime instance
            var argExpr = convertedChildren[1];       // e.g., number of days/months/years
            SqlDatePart datePart;

            switch (methodName)
            {
                case nameof(DateTime.AddYears):
                    datePart = SqlDatePart.Year;
                    break;
                case nameof(DateTime.AddMonths):
                    datePart = SqlDatePart.Month;
                    break;
                case nameof(DateTime.AddDays):
                    datePart = SqlDatePart.Day;
                    break;
                case nameof(DateTime.AddHours):
                    datePart = SqlDatePart.Hour;
                    break;
                case nameof(DateTime.AddMinutes):
                    datePart = SqlDatePart.Minute;
                    break;
                case nameof(DateTime.AddSeconds):
                    datePart = SqlDatePart.Second;
                    break;
                case nameof(DateTime.AddMilliseconds):
                    datePart = SqlDatePart.Millisecond;
                    break;
                case nameof(DateTime.AddTicks):
                    datePart = SqlDatePart.Tick; // Ticks are not directly supported, but can be converted to nanoseconds
                    break;
                default:
                    throw new NotSupportedException($"Unsupported DateTime method: {methodName}");
            }

            return this.SqlFactory.CreateDateAdd(datePart, argExpr, dateExpr);
        }
    }

}
