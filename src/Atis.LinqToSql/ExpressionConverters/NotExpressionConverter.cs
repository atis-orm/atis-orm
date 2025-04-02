﻿using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionConverters
{
    public class NotExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<UnaryExpression>
    {
        public NotExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Not)
            {
                converter = new NotExpressionConverter(Context, unaryExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class NotExpressionConverter : LinqToSqlExpressionConverterBase<UnaryExpression>
    {
        public NotExpressionConverter(IConversionContext context, UnaryExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            return this.SqlFactory.CreateNot(convertedChildren[0]);
        }
    }
}
