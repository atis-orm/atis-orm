using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public class StandaloneSelectQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        public StandaloneSelectQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                    methodCallExpression.Method.Name == nameof(QueryExtensions.Select) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new StandaloneSelectQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class StandaloneSelectQueryMethodExpressionConverter : LinqToSqlQueryConverterBase<MethodCallExpression>
    {
        public StandaloneSelectQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            if (sourceExpression == this.Expression.Arguments[0])
            {
                convertedExpression = this.SqlFactory.CreateLiteral("dummy");
                return true;
            }
            convertedExpression = null;
            return false;
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // convertedChildren[0] is dummy (for IQueryProvider)
            var convertedExpression = convertedChildren[1];
            SelectColumn[] selectColumns;
            if (convertedExpression is SqlCompositeBindingExpression compositeBinding)
            {
                selectColumns = compositeBinding.Bindings.Select(x => new SelectColumn(x.SqlExpression, x.ModelPath.GetLastElementRequired(), x.ModelPath, scalarColumn: false)).ToArray();
            }
            else
            {
                selectColumns = new[] { new SelectColumn(convertedExpression, "Col1", ModelPath.Empty, scalarColumn: true) };
            }
            
            var sqlQuery = this.SqlFactory.CreateSelectQueryFromStandaloneSelect(selectColumns);
            return sqlQuery;
        }

        /// <inheritdoc />
        public override bool IsChainedQueryArgument(Expression childNode) => false;
    }
}
