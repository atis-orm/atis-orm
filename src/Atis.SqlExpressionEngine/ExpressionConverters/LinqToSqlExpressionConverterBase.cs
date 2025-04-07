using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Abstract base class for converting LINQ expressions to SQL expressions.
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">The type of the source expression to be converted.</typeparam>
    public abstract class LinqToSqlExpressionConverterBase<TSource> : ExpressionConverterBase<Expression, SqlExpressions.SqlExpression> where TSource : Expression
    {
        /// <summary>
        ///     <para>
        ///         Gets the conversion context for the current conversion process.
        ///     </para>
        /// </summary>
        public IConversionContext Context { get; }
        public ISqlExpressionFactory SqlFactory { get; }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="LinqToSqlExpressionConverterBase{TSource}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converters">The stack of converters representing the parent chain for context-aware conversion.</param>
        public LinqToSqlExpressionConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpressions.SqlExpression>[] converters)
            : base(expression, converters)
        {
            this.Context = context;
            this.SqlFactory = this.Context.GetExtensionRequired<ISqlExpressionFactory>();
        }

        /// <summary>
        ///     <para>
        ///         Gets the source expression that is currently being converted.
        ///     </para>
        /// </summary>
        public new TSource Expression => (TSource)base.Expression;
    }
}
