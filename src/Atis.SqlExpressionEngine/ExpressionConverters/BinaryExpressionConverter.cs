using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating converters that transform binary expressions into SQL expressions.
    ///     </para>
    /// </summary>
    public class BinaryExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<BinaryExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="BinaryExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public BinaryExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                converter = new BinaryExpressionConverter(this.Context, binaryExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }
    /// <summary>
    ///     <para>
    ///         Converter class for transforming binary expressions into SQL expressions.
    ///     </para>
    /// </summary>
    public class BinaryExpressionConverter : LinqToNonSqlQueryConverterBase<BinaryExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="BinaryExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The binary expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public BinaryExpressionConverter(IConversionContext context, BinaryExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var left = convertedChildren[0];
            var right = convertedChildren[1];
            SqlBinaryExpression result = this.SqlFactory.CreateBinary(left, right, this.GetSqlExpressionType(this.Expression.NodeType));
            return result;
        }

        /// <summary>
        ///     <para>
        ///         Gets the SQL expression type corresponding to the specified binary expression type.
        ///     </para>
        /// </summary>
        /// <param name="nodeType">The type of the binary expression.</param>
        /// <returns>The corresponding SQL expression type.</returns>
        /// <exception cref="NotSupportedException">Thrown when the binary operator is not supported.</exception>
        protected virtual SqlExpressionType GetSqlExpressionType(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return SqlExpressionType.Add;
                case ExpressionType.Subtract:
                    return SqlExpressionType.Subtract;
                case ExpressionType.Multiply:
                    return SqlExpressionType.Multiply;
                case ExpressionType.Divide:
                    return SqlExpressionType.Divide;
                case ExpressionType.Modulo:
                    return SqlExpressionType.Modulus;
                case ExpressionType.LessThan:
                    return SqlExpressionType.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return SqlExpressionType.LessThanOrEqual;
                case ExpressionType.GreaterThan:
                    return SqlExpressionType.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return SqlExpressionType.GreaterThanOrEqual;
                case ExpressionType.Equal:
                    return SqlExpressionType.Equal;
                case ExpressionType.NotEqual:
                    return SqlExpressionType.NotEqual;
                case ExpressionType.AndAlso:
                    return SqlExpressionType.AndAlso;
                case ExpressionType.OrElse:
                    return SqlExpressionType.OrElse;
                case ExpressionType.Coalesce:
                    return SqlExpressionType.Coalesce;
                default:
                    throw new NotSupportedException($"The binary operator '{nodeType}' is not supported.");
            }
        }
    }
}
