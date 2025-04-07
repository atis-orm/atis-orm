using Atis.Expressions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Preprocesses casting expressions and picks the actual property using Expression's Type.
    ///     </para>
    /// </summary>
    public class ConvertExpressionReplacementPreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            var updatedNode = base.VisitMember(node);

            // ((SomeType)x).Column  where `Column` as a MemberInfo does not belong to type `x` instead it belongs to SomeType.
            // Note that, type of `x` do have `Column` member but from reflection standpoint this `Column` member is part of `SomeType`,
            // therefore, below we are testing this and picking the correct MemberInfo (`Column` property) from the actual type of `x`
            if (updatedNode is MemberExpression memberExpression &&                
                    memberExpression.Expression is UnaryExpression unaryExpr)
            {
                var actualPropertyInfo = unaryExpr.Operand.Type.GetProperty(memberExpression.Member.Name);
                if (actualPropertyInfo != null)
                    return Expression.MakeMemberAccess(unaryExpr.Operand, actualPropertyInfo);
            }

            return updatedNode;
        }
    }
}
