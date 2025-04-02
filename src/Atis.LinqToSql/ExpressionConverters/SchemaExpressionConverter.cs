using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating <see cref="SchemaExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class SchemaExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<Expression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SchemaExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public SchemaExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpr &&
                    methodCallExpr.Method.Name == nameof(QueryExtensions.Schema) &&
                    methodCallExpr.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new SchemaExpressionConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }
    /// <summary>
    ///     <para>
    ///         Converter class for converting schema method expressions to SQL expressions.
    ///     </para>
    /// </summary>
    public class SchemaExpressionConverter : LinqToSqlExpressionConverterBase<Expression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SchemaExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public SchemaExpressionConverter(IConversionContext context, Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return convertedChildren[0];
        }
    }
}
