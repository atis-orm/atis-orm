using Atis.Expressions;
using Atis.LinqToSql.Internal;
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
    ///         Abstract base class for converting collective column LINQ expressions to SQL expressions.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type of the source expression to be converted.</typeparam>
    public abstract class CollectiveColumnExpressionConverterBase<T> : CollectiveExpressionConverterBase<T> where T : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="CollectiveColumnExpressionConverterBase{T}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        protected CollectiveColumnExpressionConverterBase(IConversionContext context, T expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<SqlExpression> CreateCollection(SqlExpression[] arguments, string[] memberNames)
        {
            var projectionCreator = new ProjectionCreator(this.SqlFactory);
            return projectionCreator.Create(arguments, memberNames);
        }
    }
}
