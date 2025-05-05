using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public abstract class CompositeBindingExpressionConverterBase<T> : LinqToNonSqlQueryConverterBase<T> where T : Expression
    {
        private readonly IReflectionService reflectionService;

        protected CompositeBindingExpressionConverterBase(IConversionContext context, T expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
            this.reflectionService = this.Context.GetExtensionRequired<IReflectionService>();
        }

        protected abstract string[] GetMemberNames();
        protected abstract SqlExpression[] GetSqlExpressions(SqlExpression[] convertedChildren);
        protected abstract Type GetExpressionType(int i);

        public sealed override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var memberNames = this.GetMemberNames();
            var sqlExpressions = this.GetSqlExpressions(convertedChildren);
            var bindings = new List<SqlExpressionBinding>();
            for (var i = 0; i < sqlExpressions.Length; i++)
            {
                var sqlExpression = sqlExpressions[i];
                var modelPath = new ModelPath(memberNames[i]);
                if (sqlExpression is SqlCompositeBindingExpression compositeBinding)
                {
                    bindings.AddRange(compositeBinding.Bindings.Select(x => x.PrependPath(modelPath)));
                }
                else
                {
                    var expressionType = this.GetExpressionType(i);
                    
                    if (this.reflectionService.IsQueryableType(expressionType))
                    {
                        if (sqlExpression is SqlDerivedTableExpression derivedTableExpression)
                            sqlExpression = new SqlQueryableExpression(derivedTableExpression);
                        else
                            throw new InvalidOperationException($"When converting member '{memberNames[i]}', the expression type '{expressionType}' should have been converted to '{nameof(SqlDerivedTableExpression)}' but it was '{sqlExpression.GetType().Name}'.");
                    }
                    var sqlBinding = new SqlExpressionBinding(sqlExpression, modelPath);
                    bindings.Add(sqlBinding);
                }
            }
            return this.SqlFactory.CreateCompositeBindingForMultipleExpressions(bindings.ToArray());
        }
    }
}
