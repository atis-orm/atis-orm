using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{
    public partial class ChildJoinReplacementPreprocessor
    {
        /// <summary>
        ///     <para>
        ///         This class is being used in <see cref="ChildJoinReplacementPreprocessor"/> to remove redundant <c>true</c> conditions from the
        ///         original expression tree.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         In <c>SelectMany</c> call, if the query is converted to <see cref="ExpressionExtensions.ChildJoinExpression"/>, 
        ///         then the predicates within that query are checked and replaced with constant <c>true</c> condition.
        ///     </para>
        ///     <para>
        ///         This class looks for those constants and removes them from the expression tree.
        ///     </para>
        ///     <para>
        ///         Caution: this class is not intended to be used by the end user and is not guaranteed
        ///         to be available in future versions.
        ///     </para>
        /// </remarks>
        private class RemoveRedundantTrueVisitor : ExpressionVisitor
        {
            /// <inheritdoc />
            protected override Expression VisitBinary(BinaryExpression node)
            {
                // Remove 'true' conditions from logical AND operations
                if (node.NodeType == ExpressionType.AndAlso)
                {
                    if (node.Left is ConstantExpression leftConstant && leftConstant.Value is bool b1 && b1)
                    {
                        return Visit(node.Right);
                    }
                    if (node.Right is ConstantExpression rightConstant && rightConstant.Value is bool b2 && b2)
                    {
                        return Visit(node.Left);
                    }
                }

                return base.VisitBinary(node);
            }

            /// <inheritdoc />
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // Handle the 'Where' method calls
                if (node.Method.Name == nameof(Queryable.Where) && node.Arguments.Count >= 2)
                {
                    if (node.Arguments[1] is UnaryExpression unaryExpression && unaryExpression.Operand is LambdaExpression lambda)
                    {
                        // Check if the lambda is `x => true`
                        if (lambda.Body is ConstantExpression constant && constant.Value is bool b1 && b1)
                        {
                            // Skip this 'Where' call
                            return Visit(node.Arguments[0]);
                        }
                    }
                }

                return base.VisitMethodCall(node);
            }
        }
    }

}
