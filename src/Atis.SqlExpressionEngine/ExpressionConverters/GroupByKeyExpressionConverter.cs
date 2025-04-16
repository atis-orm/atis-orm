using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating GroupByKeyExpressionConverter instances.
    ///     </para>
    /// </summary>
    public class GroupByKeyExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="GroupByKeyExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public GroupByKeyExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }


        /// <summary>
        ///     <para>
        ///         Determines whether the specified member expression is a GroupBy key member.
        ///     </para>
        /// </summary>
        /// <param name="memberExpression">The member expression to check.</param>
        /// <returns><c>true</c> if the specified member expression is a GroupBy key member; otherwise, <c>false</c>.</returns>
        protected virtual bool IsGroupByKeyMember(MemberExpression memberExpression)
        {
            if (memberExpression.Member.Name == "Key")
            {
                var parentType = memberExpression.Expression.Type;
                // TODO: see if we can move this part to IReflectionService
                if (parentType.IsGenericType && parentType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression memberExpr)
            {
                if (this.IsGroupByKeyMember(memberExpr)
                    ||
                    (memberExpr.Expression is MemberExpression parentOfMemberExpr && this.IsGroupByKeyMember(parentOfMemberExpr)))
                {
                    converter = new GroupByKeyExpressionConverter(this.Context, memberExpr, converterStack);
                    return true;
                }
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for converting GroupBy key expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public class GroupByKeyExpressionConverter : LinqToSqlExpressionConverterBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="GroupByKeyExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The expression to be converted.</param>
        /// <param name="converterStack">The stack of converters in use.</param>
        public GroupByKeyExpressionConverter(IConversionContext context, MemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var child = convertedChildren[0];
            var sqlQuery = (child as SqlQueryReferenceExpression)?.Reference
                            ??
                            // TODO: check if we can ever receive direct SqlQueryExpression
                            child as SqlQueryExpression
                            ??
                            throw new InvalidOperationException($"Expected {nameof(SqlQueryExpression)} on the stack");
            // Case-1: x.Key
            // Case-2: x.Key.Name
            // if we are here it means either of the 2 cases are true
            var isLeafNode = !(this.ParentConverter is GroupByKeyExpressionConverter);

            if (this.Expression.Member.Name == "Key")
            {
                // x.Key
                //  it means, user has done scalar grouping
                if (isLeafNode)
                {
                    return sqlQuery.GetGroupByScalarExpression()
                            ??
                            sqlQuery.GroupBy;
                }
                else
                    return sqlQuery;
            }
            else if (this.Expression.Expression is MemberExpression parentOfMemberExpr)
            {
                if (parentOfMemberExpr.Member.Name == "Key")
                {
                    // x.Key.Field
                    return sqlQuery.GetGroupByExpression(this.Expression.Member.Name)
                            ??
                            throw new InvalidOperationException($"Expression '{this.Expression}' is a GroupBy Expression which should return an expression for Key '{this.Expression.Member.Name}', but no value was returned.");
                }
            }

            throw new InvalidOperationException($"Expression '{this.Expression}' is a GroupBy Expression which should return an expression for Key '{this.Expression.Member.Name}', but no value was returned.");
        }
    }
}
