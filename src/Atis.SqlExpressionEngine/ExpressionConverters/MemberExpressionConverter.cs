using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Exceptions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.IO;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating converters that handle MemberExpression instances.
    ///     </para>
    /// </summary>
    public class MemberExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public MemberExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression memberExpression)
            {
                converter = new MemberExpressionConverter(this.Context, memberExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for handling MemberExpression instances and converting them to SQL expressions.
    ///     </para>
    /// </summary>
    public class MemberExpressionConverter : LinqToNonSqlQueryConverterBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The MemberExpression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public MemberExpressionConverter(IConversionContext context, MemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether this instance is a leaf node in the <c>MemberExpression</c> chain.
        ///     </para>
        /// </summary>
        protected virtual bool IsLeafNode => this.ParentConverter?.GetType() != this.GetType();

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // although the `Expression` part of a MemberExpression can be said as Child node of MemberExpression,
            // but we are naming it as `parent` below because it can be considered as the parent of the member.
            var parent = convertedChildren[0];

            if (parent is SqlQueryShapeExpression queryShape)
            {
                if (!queryShape.TryResolveMember(this.Expression.Member.Name, out var resolvedExpression))
                    throw new UnresolvedMemberAccessException(this.Expression.GetPath());
                if (this.IsLeafNode && resolvedExpression is SqlQueryShapeFieldResolverExpression fieldResolver)
                    resolvedExpression = fieldResolver.ShapeExpression;
                return resolvedExpression;
            }
            else if (parent is SqlDerivedTableExpression derivedTable)
            {
                var top = derivedTable.Top;
                var distinct = derivedTable.IsDistinct;
                var rowOffset = derivedTable.RowOffset;
                var rowsPerPage = derivedTable.RowsPerPage;
                if (top != null || distinct || rowOffset != null || rowsPerPage != null)
                {
                    derivedTable = new SqlDerivedTableExpression(derivedTable.CteDataSources, derivedTable.FromSource, derivedTable.Joins, derivedTable.WhereClause, derivedTable.GroupByClause, derivedTable.HavingClause, derivedTable.OrderByClause, derivedTable.SelectColumnCollection, false, null, null, null, derivedTable.AutoProjection, derivedTable.Tag, derivedTable.QueryShape, derivedTable.NodeType);
                }
                var sqlQuery = this.SqlFactory.CreateSelectQuery(derivedTable);
                var sqlQueryShape = sqlQuery.GetQueryShapeForFieldMapping().CastTo<SqlQueryShapeFieldResolverExpression>();
                if (!sqlQueryShape.TryResolveMember(this.Expression.Member.Name, out var resolved2))
                    throw new UnresolvedMemberAccessException(this.Expression.GetPath());
                if (resolved2 is SqlQueryShapeFieldResolverExpression fieldResolver)
                    resolved2 = fieldResolver.ShapeExpression;
                sqlQuery.ApplyProjection(resolved2);
                if (top != null)
                    sqlQuery.ApplyTop(top.Value);
                if (distinct)
                    sqlQuery.ApplyDistinct();
                if (rowOffset != null)
                    sqlQuery.ApplyRowOffset(rowOffset.Value);
                if (rowsPerPage != null)
                    sqlQuery.ApplyRowsPerPage(rowsPerPage.Value);
                return this.SqlFactory.ConvertSelectQueryToDeriveTable(sqlQuery);
            }

            throw new NotImplementedException();
        }
    }
}
