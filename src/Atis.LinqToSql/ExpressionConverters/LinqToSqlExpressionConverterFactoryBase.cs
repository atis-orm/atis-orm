using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Abstract base class for creating expression converters that transform LINQ expressions to SQL expressions.
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">The type of the source expression.</typeparam>
    public abstract class LinqToSqlExpressionConverterFactoryBase<TSource> : IExpressionConverterFactory<Expression, SqlExpression> where TSource : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="LinqToSqlExpressionConverterFactoryBase{TSource}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public LinqToSqlExpressionConverterFactoryBase(IConversionContext context)
        {
            this.Context = context;
        }

        /// <summary>
        ///     <para>
        ///         Gets the conversion context.
        ///     </para>
        /// </summary>
        public IConversionContext Context { get; }

        /// <summary>
        ///     <para>
        ///         Attempts to create an expression converter for the specified source expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">The source expression for which the converter is being created.</param>
        /// <param name="converterStack">The current stack of converters in use, which may influence the creation of the new converter.</param>
        /// <param name="converter">When this method returns, contains the created expression converter if the creation was successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a suitable converter was successfully created; otherwise, <c>false</c>.</returns>
        public abstract bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter);
    }
}
