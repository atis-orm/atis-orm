using Atis.Expressions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Abstract base class for converting collective LINQ expressions to SQL expressions.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type of the source expression to be converted.</typeparam>
    public abstract class CollectiveExpressionConverterBase<T> : LinqToSqlExpressionConverterBase<T> where T : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="CollectiveExpressionConverterBase{T}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        protected CollectiveExpressionConverterBase(IConversionContext context, T expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var arguments = this.GetSqlExpressions(convertedChildren);
            var memberNames = this.GetMemberNames();
            var collection = this.CreateCollection(arguments, memberNames);
            return new SqlCollectionExpression(collection);
        }

        /// <summary>
        /// Creates a collection of SQL expressions.
        /// </summary>
        /// <param name="arguments">The SQL expressions to include in the collection.</param>
        /// <param name="memberNames">The member names associated with the SQL expressions.</param>
        /// <returns>A collection of SQL expressions.</returns>
        protected abstract IEnumerable<SqlExpression> CreateCollection(SqlExpression[] arguments, string[] memberNames);

        /// <summary>
        /// Gets the SQL expressions from the stack.
        /// </summary>
        /// <param name="convertedChildren">The stack containing SQL expressions.</param>
        /// <returns>An array of SQL expressions.</returns>
        protected abstract SqlExpression[] GetSqlExpressions(SqlExpression[] convertedChildren);

        /// <summary>
        /// Gets the member names associated with the SQL expressions.
        /// </summary>
        /// <returns>An array of member names.</returns>
        protected abstract string[] GetMemberNames();
    }
}
