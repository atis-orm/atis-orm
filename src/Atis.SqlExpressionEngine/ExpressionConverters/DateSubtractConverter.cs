using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class DateSubtractConverterFacotry : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        private static readonly HashSet<string> supportedMembers = new HashSet<string>(
            new[] {
                nameof(TimeSpan.Days),
                nameof(TimeSpan.Hours),
                nameof(TimeSpan.Minutes),
                nameof(TimeSpan.Seconds),
                nameof(TimeSpan.Milliseconds),
                nameof(TimeSpan.Ticks),
            });

        public DateSubtractConverterFacotry(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression memberExpression &&
                supportedMembers.Contains(memberExpression.Member.Name) &&
                memberExpression.Expression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.DeclaringType == typeof(DateTime) &&
                methodCallExpression.Method.Name == nameof(DateTime.Subtract))
            {
                converter = new DateSubtractConverter(this.Context, memberExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class DateSubtractConverter : LinqToSqlExpressionConverterBase<MemberExpression>
    {
        public DateSubtractConverter(IConversionContext context, MemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        /// <inheritdoc/>
        public override bool TryCreateChildConverter(Expression childNode, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> childConverter)
        {
            if (childNode == this.Expression.Expression && childNode is MethodCallExpression methodCallExpression)
            {
                childConverter = new DateSubtractMethodCallConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            return base.TryCreateChildConverter(childNode, converterStack, out childConverter);
        }

        /// <inheritdoc/>
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            if (convertedChildren.Length == 0)
                throw new InvalidOperationException($"No children were converted, expected at-least 1.");
            var collection = convertedChildren[0] as SqlCollectionExpression
                            ??
                            throw new InvalidOperationException($"Expected a collection, but got {convertedChildren[0].GetType()}.");

            if (collection.SqlExpressions.Count() < 2)
                throw new ArgumentException($"Expected 2 children, but got {convertedChildren.Length}.");

            // dateField.Subtract(otherField).Seconds
            var dateStart = collection.SqlExpressions.ElementAt(0);
            var dateEnd = collection.SqlExpressions.ElementAt(1);
            SqlDatePart datePart;
            switch(this.Expression.Member.Name)
            {
                case nameof(TimeSpan.Days):
                    datePart = SqlDatePart.Day;
                    break;
                case nameof(TimeSpan.Hours):
                    datePart = SqlDatePart.Hour;
                    break;
                case nameof(TimeSpan.Minutes):
                    datePart = SqlDatePart.Minute;
                    break;
                case nameof(TimeSpan.Seconds):
                    datePart = SqlDatePart.Second;
                    break;
                case nameof(TimeSpan.Milliseconds):
                    datePart = SqlDatePart.Millisecond;
                    break;
                case nameof(TimeSpan.Ticks):
                    datePart = SqlDatePart.Tick;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member: {this.Expression.Member.Name}");
            }
            return this.SqlFactory.CreateDateSubtract(datePart, dateStart, dateEnd);
        }

        private class DateSubtractMethodCallConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
        {
            public DateSubtractMethodCallConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
            {
            }

            /// <inheritdoc/>
            public override SqlExpression Convert(SqlExpression[] convertedChildren)
            {
                // dateField.Subtract(otherField)
                return this.SqlFactory.CreateCollection(convertedChildren);
            }
        }
    }
}
