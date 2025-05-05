using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public interface ILinqToSqlExpressionConverterBase
    {
        bool IsQueryConverter { get; }
        bool IsChainedQueryArgument(Expression childNode);
    }

    /// <summary>
    ///     <para>
    ///         Abstract base class for converting LINQ expressions to SQL expressions.
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">The type of the source expression to be converted.</typeparam>
    public abstract class LinqToSqlExpressionConverterBase<TSource> : ExpressionConverterBase<Expression, SqlExpression>, ILinqToSqlExpressionConverterBase where TSource : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="LinqToNonSqlQueryConverterBase{TSource}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converters">The stack of converters representing the parent chain for context-aware conversion.</param>
        protected LinqToSqlExpressionConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpression>[] converters)
            : base(expression, converters)
        {
            this.Context = context;
            this.SqlFactory = this.Context.GetExtensionRequired<ISqlExpressionFactory>();
        }

        /// <summary>
        ///     <para>
        ///         Gets the conversion context for the current conversion process.
        ///     </para>
        /// </summary>
        public IConversionContext Context { get; }
        /// <summary>
        /// 
        /// </summary>
        public ISqlExpressionFactory SqlFactory { get; }
        /// <summary>
        ///     <para>
        ///         Gets the source expression that is currently being converted.
        ///     </para>
        /// </summary>
        public new TSource Expression => (TSource)base.Expression;

        public abstract bool IsQueryConverter { get; }
        public abstract bool IsChainedQueryArgument(Expression childNode);
        public abstract SqlExpression Convert(SqlExpression[] convertedChildren);

        public virtual void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            // no default implementation
        }

        /// <inheritdoc />
        public sealed override SqlExpression TransformConvertedChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (this.IsQueryConverter)      // if this method is a query converter e.g. Select
            {
                // if converted child node should be a SqlSelectExpression but it's NOT
                if (this.IsChainedQueryArgument(childNode) && !(convertedExpression is SqlSelectExpression))
                {
                    if (convertedExpression is SqlQuerySourceExpression querySource)
                    {
                        convertedExpression = this.SqlFactory.CreateSelectQueryFromQuerySource(querySource);
                    }
                    else if (convertedExpression is SqlQueryableExpression queryable)
                    {
                        convertedExpression = this.SqlFactory.CreateSelectQueryFromQuerySource(queryable.Query);
                    }
                    else
                        throw new InvalidOperationException($"Converter '{this.GetType().Name}' has been marked as Query Converter, also the converter is suggesting that childNode '{childNode}' will be a '{nameof(SqlSelectExpression)}' but it's not, so the core engine is trying to create '{nameof(SqlSelectExpression)}' from converted child '{convertedExpression.GetType().Name}' but it's not '{nameof(SqlQuerySourceExpression)}'. The child converter '{childConverter.GetType().Name}' should convert the node '{childNode}' to either '{nameof(SqlSelectExpression)}' or '{nameof(SqlQuerySourceExpression)}'.");
                }
            }
            this.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
            return convertedExpression;
        }

        /// <inheritdoc />
        public sealed override SqlExpression CreateFromChildren(SqlExpression[] convertedChildren)
        {
            var convertedResult = this.Convert(convertedChildren);
            if (this.ParentConverter is null        // if this is the top-most node
                ||                                  //  or
                                                    // if the parent is not a query converter
                this.ParentConverter is ILinqToSqlExpressionConverterBase parentConverter && 
                    (
                        !parentConverter.IsQueryConverter ||
                        // it means parentConverter is a query converter but we cannot treat this child
                        // as chained query if it's not the one which should be converted
                        !parentConverter.IsChainedQueryArgument(this.Expression)
                    )
                )
            {
                // if this converter is a Query Converter e.g. Select method
                if (this.IsQueryConverter)
                {
                    // now we need to close the query, because either this is the top-most node or
                    // parent is not a query converter or parent is the query converter but this
                    // child is not the chained query parameter
                    // e.g. Join(querySource, otherDataSource, PK selection, FK selection, new shape)
                    // in above call `otherDataSource` will also be translated to SqlSelectExpression but
                    // it should be closed after conversion of that node because `otherDataSource` is not the
                    // chained argument to parent (Join)
                    if (convertedResult is SqlSelectExpression selectQuery)
                    {
                        selectQuery.ApplyAutoProjection();
                        var derivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(selectQuery);
                        convertedResult = derivedTable;
                    }
                }
            }
            return convertedResult;
        }
    }

    public abstract class LinqToSqlQueryConverterBase<TSource> : LinqToSqlExpressionConverterBase<TSource> where TSource : Expression
    {
        protected LinqToSqlQueryConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public sealed override bool IsQueryConverter => true;
    }

    public abstract class LinqToNonSqlQueryConverterBase<TSource> : LinqToSqlExpressionConverterBase<TSource> where TSource : Expression
    {
        protected LinqToNonSqlQueryConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public sealed override bool IsQueryConverter => false;
        public sealed override bool IsChainedQueryArgument(Expression childNode) => false;
    }
}
