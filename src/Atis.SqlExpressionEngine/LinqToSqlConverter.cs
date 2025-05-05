using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Internal;
using Atis.SqlExpressionEngine.Services;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine
{
    public class LinqToSqlConverter : ILinqToSqlConverter
    {
        private readonly LinqToSqlConverterInternal linqToSqlConverterInternal;

        public LinqToSqlConverter(IReflectionService reflectionService, IExpressionConverterProvider<Expression, SqlExpression> expressionConverterProvider, ISqlExpressionPostprocessorProvider postProcessorProvider)
        {
            this.linqToSqlConverterInternal = new LinqToSqlConverterInternal(reflectionService, expressionConverterProvider, postProcessorProvider);
        }

        public virtual SqlExpression Convert(Expression expression)
        {
            var sqlExpression = linqToSqlConverterInternal.Convert(expression);
            return sqlExpression;
        }

        private class LinqToSqlConverterInternal : ExpressionVisitor
        {
            private readonly ExpressionConverterVisitor<Expression, SqlExpression> visitor;
            private readonly IExpressionConverterProvider<Expression, SqlExpression> expressionConverterProvider;

            private readonly ISqlExpressionPostprocessorProvider postprocessorProvider;

            public LinqToSqlConverterInternal(IReflectionService reflectionService, IExpressionConverterProvider<Expression, SqlExpression> expressionConverterProvider, ISqlExpressionPostprocessorProvider postProcessorProvider)
            {
                this.visitor = new ExpressionConverterVisitor<Expression, SqlExpression>(expressionConverterProvider);
                this.postprocessorProvider = postProcessorProvider;
                this.expressionConverterProvider = expressionConverterProvider;
            }

            public SqlExpression Convert(Expression expression)
            {
                this.visitor.Initialize();

                this.Visit(expression);

                var sqlExpression = this.visitor.GetConvertedExpression();

                if (this.postprocessorProvider != null)
                    sqlExpression = this.postprocessorProvider.Postprocess(sqlExpression);

                return sqlExpression;
            }

            /// <inheritdoc />
            public sealed override Expression Visit(Expression node)
            {
                if (node is null) return node;

                var expr = this.visitor.Visit(node, base.Visit);

                return expr;
            }
        }
    }
}
