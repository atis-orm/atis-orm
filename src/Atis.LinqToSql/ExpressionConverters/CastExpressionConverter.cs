using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating instances of <see cref="CastExpressionConverter"/>.
    ///     </para>
    /// </summary>
    public class CastExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<UnaryExpression>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CastExpressionConverterFactory"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public CastExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is UnaryExpression unaryExpression && expression.NodeType == ExpressionType.Convert)
            {
                converter = new CastExpressionConverter(this.Context, unaryExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }
    /// <summary>
    ///     <para>
    ///         Converter class for converting <see cref="UnaryExpression"/> to <see cref="SqlExpression"/>.
    ///     </para>
    /// </summary>
    public class CastExpressionConverter : LinqToSqlExpressionConverterBase<UnaryExpression>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CastExpressionConverter"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The unary expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public CastExpressionConverter(IConversionContext context, UnaryExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var lastExpr = convertedChildren[0];
            return lastExpr;
        }
    }
}
