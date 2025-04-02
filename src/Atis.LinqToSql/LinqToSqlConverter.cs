using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.Internal;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.LinqToSql
{
    public class LinqToSqlConverter : ILinqToSqlConverter
    {
        private readonly LinqToSqlConverterInternal linqToSqlConverterInternal;

        public LinqToSqlConverter(IReflectionService reflectionService, IExpressionConverterProvider<Expression, SqlExpression> expressionConverterProvider, IPostprocessorProvider postProcessorProvider)
        {
            this.linqToSqlConverterInternal = new LinqToSqlConverterInternal(reflectionService, expressionConverterProvider, postProcessorProvider, this.OnQueryClosed);
        }

        public virtual SqlExpression Convert(Expression expression)
        {
            var sqlExpression = this.linqToSqlConverterInternal.Convert(expression);
            return sqlExpression;
        }

        protected virtual void OnQueryClosed(SqlQueryExpression sqlQuery)
        {
            sqlQuery.ApplyAutoProjection();
        }

        private class LinqToSqlConverterInternal : ExpressionVisitor
        {
            private readonly ExpressionConverterVisitor<Expression, SqlExpression> visitor;
            private readonly LinqQueryContextManager linqQueryManager;
            private readonly IPostprocessorProvider postprocessorProvider;

            public LinqToSqlConverterInternal(IReflectionService reflectionService, IExpressionConverterProvider<Expression, SqlExpression> expressionConverterProvider, IPostprocessorProvider postProcessorProvider, Action<SqlQueryExpression> onQueryClosed)
            {
                this.visitor = new ExpressionConverterVisitor<Expression, SqlExpression>(expressionConverterProvider);
                this.linqQueryManager = new LinqQueryContextManager(reflectionService)
                {
                    OnQueryClosed = this.OnQueryClosed
                };
                this.postprocessorProvider = postProcessorProvider;
                this.OnSqlQueryClosed = onQueryClosed;
            }

            public SqlExpression Convert(Expression expression)
            {
                this.visitor.Initialize();

                this.Visit(expression);

                var sqlExpression = this.visitor.GetConvertedExpression();

                if (this.postprocessorProvider != null)
                    sqlExpression = this.postprocessorProvider.Process(sqlExpression);

                return sqlExpression;
            }

            /// <inheritdoc />
            public sealed override Expression Visit(Expression node)
            {
                if (node is null) return node;

                this.linqQueryManager.BeforeVisit(node);
                var expr = this.visitor.Visit(node, base.Visit);
                // cannot pass `expr` in below method, although `expr` is not going to change
                // because the converter is not going to change the original expression tree
                // it's just traversing through the tree, but still we are going to pass
                // the original `node`
                this.linqQueryManager.AfterVisit(node);
                return expr;
            }

            private void OnQueryClosed(LinqQuery linqQuery, Expression node)
            {
                if (this.visitor.LastConversion is SqlQueryExpression sqlQuery)
                {
                    this.OnSqlQueryClosed?.Invoke(sqlQuery);
                }
            }

            public Action<SqlQueryExpression> OnSqlQueryClosed { get; set; }
        }
    }
}
