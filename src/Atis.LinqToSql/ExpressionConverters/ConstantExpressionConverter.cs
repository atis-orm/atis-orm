using Atis.Expressions;
using Atis.LinqToSql.ContextExtensions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating converters that handle constant expressions.
    ///     </para>
    /// </summary>
    public class ConstantExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<ConstantExpression>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantExpressionConverterFactory"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public ConstantExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is ConstantExpression constExpression)
            {
                converter = new ConstantExpressionConverter(this.Context, constExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for transforming constant expressions to SQL parameter expressions.
    ///     </para>
    /// </summary>
    public class ConstantExpressionConverter : LinqToSqlExpressionConverterBase<ConstantExpression>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantExpressionConverter"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="constantExpression">The constant expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public ConstantExpressionConverter(IConversionContext context, ConstantExpression constantExpression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, constantExpression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return new SqlParameterExpression(this.Expression.Value);
        }
    }
}
