using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class NavigationMemberExpressionConverterFacotry : LinqToSqlExpressionConverterFactoryBase<NavigationMemberExpression>
    {
        public NavigationMemberExpressionConverterFacotry(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is NavigationMemberExpression navMember)
            {
                converter = new NavigationMemberExpressionConverter(this.Context, navMember, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class NavigationMemberExpressionConverter : LinqToNonSqlQueryConverterBase<NavigationMemberExpression>
    {
        public NavigationMemberExpressionConverter(IConversionContext context, NavigationMemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) 
            : base(context, expression, converters)
        {
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var queryShape = convertedChildren[0].CastTo<SqlQueryShapeExpression>();
            if (!queryShape.TryResolveMember(this.Expression.NavigationProperty, out var resolved))
                throw new InvalidOperationException($"The member '{this.Expression.NavigationProperty}' could not be resolved.");
            return resolved;
        }
    }
}
