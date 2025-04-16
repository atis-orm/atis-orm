using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class DateTimeMemberAccessConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        private static readonly string[] SupportedProperties = new[]
        {
            nameof(DateTime.Year),
            nameof(DateTime.Month),
            nameof(DateTime.DayOfWeek),
            nameof(DateTime.Day),
            nameof(DateTime.Hour),
            nameof(DateTime.Minute),
            nameof(DateTime.Second),
            nameof(DateTime.Millisecond),
            nameof(DateTime.Ticks),
            nameof(DateTime.Date),
        };

        public DateTimeMemberAccessConverterFactory(IConversionContext context) : base(context) { }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression member &&
                member.Member.DeclaringType == typeof(DateTime) &&
                SupportedProperties.Contains(member.Member.Name))
            {
                converter = new DateTimeMemberAccessConverter(this.Context, member, converterStack);
                return true;
            }

            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converts DateTime properties like Year, Month, Day, Hour, etc., into SQL DATEPART function calls.
    ///     </para>
    /// </summary>
    public class DateTimeMemberAccessConverter : LinqToSqlExpressionConverterBase<MemberExpression>
    {
        private static readonly Dictionary<string, SqlDatePart> PropertyToDatePart = new Dictionary<string, SqlDatePart>()
        {
            [nameof(DateTime.Year)] = SqlDatePart.Year,
            [nameof(DateTime.Month)] = SqlDatePart.Month,
            [nameof(DateTime.Day)] =  SqlDatePart.Day,
            [nameof(DateTime.Hour)] =  SqlDatePart.Hour,
            [nameof(DateTime.Minute)] =  SqlDatePart.Minute,
            [nameof(DateTime.Second)] =  SqlDatePart.Second,
            [nameof(DateTime.Millisecond)] =  SqlDatePart.Millisecond,
            [nameof(DateTime.Ticks)] = SqlDatePart.Tick,
            [nameof(DateTime.DayOfWeek)] = SqlDatePart.WeekDay,
        };
        private readonly ISqlDataTypeFactory sqlDataTypeFactory;

        public DateTimeMemberAccessConverter(
            IConversionContext context,
            MemberExpression expression,
            ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.sqlDataTypeFactory = this.Context.GetExtensionRequired<ISqlDataTypeFactory>();
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var dateExpr = convertedChildren[0]; // The object on which the property is accessed

            if (this.Expression.Member.Name == nameof(DateTime.Date))
            {
                return this.SqlFactory.CreateCast(dateExpr, this.sqlDataTypeFactory.CreateDate());
            }
            else
            {
                if (!PropertyToDatePart.TryGetValue(this.Expression.Member.Name, out var datePart))
                    throw new NotSupportedException($"The property '{this.Expression.Member.Name}' is not supported.");
                SqlExpression datePartExpression = this.SqlFactory.CreateDatePart(datePart, dateExpr);
                return datePartExpression;
            }
        }
    }
}
