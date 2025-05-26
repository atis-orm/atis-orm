using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CompositeMemberAssignmentConverterBase<T> : LinqToNonSqlQueryConverterBase<T> where T : Expression
    {
        protected CompositeMemberAssignmentConverterBase(IConversionContext context, T expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        protected abstract string[] GetMemberNames();
        protected abstract SqlExpression[] GetSqlExpressions(SqlExpression[] convertedChildren);
        protected abstract Type GetExpressionType(int i);

        public sealed override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var memberNames = this.GetMemberNames();
            var sqlExpressions = this.GetSqlExpressions(convertedChildren);
            if (memberNames.Length != sqlExpressions.Length)
                throw new InvalidOperationException($"The number of member names '{memberNames.Length}' does not match the number of SQL expressions '{sqlExpressions.Length}'.");
            var bindings = new List<SqlMemberAssignment>();
            for (var i = 0; i < sqlExpressions.Length; i++)
            {
                var sqlExpression = sqlExpressions[i];
                var expressionType = this.GetExpressionType(i);

                if (this.ReflectionService.IsQueryableType(expressionType))
                {
                    if (sqlExpression is SqlDerivedTableExpression derivedTableExpression)
                        sqlExpression = new SqlQueryableExpression(derivedTableExpression);
                    else
                        throw new InvalidOperationException($"When converting member '{memberNames[i]}', the expression type '{expressionType}' should have been converted to '{nameof(SqlDerivedTableExpression)}' but it was '{sqlExpression.GetType().Name}'.");
                }
                var sqlBinding = new SqlMemberAssignment(memberNames[i], sqlExpression);
                bindings.Add(sqlBinding);
            }
            return new SqlMemberInitExpression(bindings);
        }
    }
}
