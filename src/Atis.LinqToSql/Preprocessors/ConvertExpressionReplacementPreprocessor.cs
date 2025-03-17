using Atis.Expressions;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Preprocesses casting expressions and picks the actual property using Expression's Type.
    ///     </para>
    /// </summary>
    public class ConvertExpressionReplacementPreprocessor : IExpressionPreprocessor
    {
        /// <inheritdoc />
        public void BeforeVisit(Expression node, Expression[] expressionsStack)
        {
            // do nothing
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node, Expression[] expressionsStack)
        {
            // ((SomeType)x).Column  where `Column` as a MemberInfo does not belong to type `x` instead it belongs to SomeType.
            // Note that, type of `x` do have `Column` member but from reflection standpoint this `Column` member is part of `SomeType`,
            // therefore, below we are testing this and picking the correct MemberInfo (`Column` property) from the actual type of `x`
            if (node is MemberExpression memberExpr && memberExpr.Expression is UnaryExpression unaryExpr)
            {
                var actualPropertyInfo = unaryExpr.Operand.Type.GetProperty(memberExpr.Member.Name);
                if (actualPropertyInfo != null)
                    return Expression.MakeMemberAccess(unaryExpr.Operand, actualPropertyInfo);
            }
            return node;
        }
    }
}
