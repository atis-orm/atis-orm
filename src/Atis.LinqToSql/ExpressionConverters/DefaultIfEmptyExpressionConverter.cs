using Atis.Expressions;
using Atis.LinqToSql.ExpressionExtensions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory class for creating <see cref="DefaultIfEmptyExpressionConverter"/> instances.
    ///     </para>
    /// </summary>
    public class DefaultIfEmptyExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="DefaultIfEmptyExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public DefaultIfEmptyExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression && 
                methodCallExpression.Method.Name == nameof(Queryable.DefaultIfEmpty))
            {
                converter = new DefaultIfEmptyExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for handling <see cref="Queryable.DefaultIfEmpty{TSource}(IQueryable{TSource})"/> method calls.
    ///     </para>
    /// </summary>
    public class DefaultIfEmptyExpressionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="DefaultIfEmptyExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public DefaultIfEmptyExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var source = convertedChildren[0];

            SqlExpression sourceExpression;

            if (source is SqlSelectedCollectionExpression selectedColExpr)
                sourceExpression = selectedColExpr.SourceExpression;
            else
                sourceExpression = source;

            if (sourceExpression is SqlDataSourceReferenceExpression dsRef)
            {
                if (dsRef.DataSource is SqlDataSourceExpression ds)
                    ds.DataSource.IsDefaultIfEmpty = true;
                else if (dsRef.DataSource is SqlQuerySourceExpression sqlQ)
                    sqlQ.IsDefaultIfEmpty = true;
                return sourceExpression;
            }
            else
            {
                var sqlQuery = sourceExpression as SqlQuerySourceExpression
                                ??
                                throw new InvalidOperationException($"sourceExpression is not a {nameof(SqlQuerySourceExpression)}");

                sqlQuery.IsDefaultIfEmpty = true;

                return sqlQuery;
            }
        }
    }
}
