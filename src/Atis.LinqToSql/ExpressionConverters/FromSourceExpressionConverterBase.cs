using Atis.Expressions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Abstract base class for to convert different <see cref="QueryExtensions.From{T}(IQueryProvider, Expression{Func{T}})"/> 
    ///     </para>
    /// method calls.
    /// </summary>
    /// <typeparam name="T">The type of the source expression.</typeparam>
    /// <remarks>
    ///     <para>
    ///         In <see cref="QueryExtensions.From{T}(IQueryProvider, Expression{Func{T}})"/> method call, the source expression is the second argument.
    ///         This argument can be of different types like <see cref="MemberInitExpression"/>, <see cref="NewExpression"/> etc.
    ///         This class provides a base implementation to convert these different types of source expressions.
    ///     </para>
    /// </remarks>
    public abstract class FromSourceExpressionConverterBase<T> : CollectiveExpressionConverterBase<T> where T : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="FromSourceExpressionConverterBase{T}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public FromSourceExpressionConverterBase(IConversionContext context, T expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<SqlExpression> CreateCollection(SqlExpression[] arguments, string[] memberNames)
        {
            List<SqlDataSourceExpression> sourceExpressions = new List<SqlDataSourceExpression>();
            for (var i = 0; i < memberNames.Length; i++)
            {
                var memberName = memberNames[i];
                var tableOrSubQuery = arguments[i] as SqlQuerySourceExpression
                                        ??
                                        throw new InvalidOperationException($"Expected a SqlQuerySourceExpression but got {arguments[i].GetType().Name}");
                tableOrSubQuery = tableOrSubQuery.ConvertToTableIfPossible();
                if (tableOrSubQuery is SqlQueryExpression sqlQuery)
                {
                    if (sqlQuery.Projection == null)
                        throw new InvalidOperationException($"Projection has not been applied to the SqlQueryExpression at index {i}");
                }
                // TODO: add the member name some how in the navigation
                var sqlFromSourceExpression = this.SqlFactory.CreateFromSource(tableOrSubQuery, new ModelPath(memberName));
                sourceExpressions.Add(sqlFromSourceExpression);
            }
            return sourceExpressions;
        }
    }
}
