using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class DatePropertyAccessConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        private static readonly string[] SupportedProperties = new []
        {
            "Year",
            "Month",
            "Day",
            "Hour",
            "Minute",
            "Second",
            "Millisecond",
        };

        public DatePropertyAccessConverterFactory(IConversionContext context) : base(context) { }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression member &&
                member.Member.DeclaringType == typeof(DateTime) &&
                SupportedProperties.Contains(member.Member.Name))
            {
                converter = new DatePropertyAccessConverter(this.Context, member, converterStack);
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
    public class DatePropertyAccessConverter : LinqToSqlExpressionConverterBase<MemberExpression>
    {
        private static readonly Dictionary<string, string> PropertyToDatePart = new Dictionary<string, string>()
        {
            ["Year"] = "year",
            ["Month"] = "month",
            ["Day"] = "day",
            ["Hour"] = "hour",
            ["Minute"] = "minute",
            ["Second"] = "second",
            ["Millisecond"] = "millisecond"
        };

        public DatePropertyAccessConverter(
            IConversionContext context,
            MemberExpression expression,
            ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var part = PropertyToDatePart[this.Expression.Member.Name];
            var dateExpr = convertedChildren[0]; // The object on which the property is accessed

            return this.SqlFactory.CreateFunctionCall("datepart",
                new[] { this.SqlFactory.CreateKeyword(part), dateExpr });
        }
    }
}
