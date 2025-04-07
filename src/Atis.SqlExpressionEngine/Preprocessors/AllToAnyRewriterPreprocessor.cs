using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Atis.Expressions;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Rewrites all calls to `.All(...)` into `!Any(...)` with a negated predicate.
    ///         That is: `x.All(p => condition)` becomes `!x.Any(p => !condition)`
    ///     </para>
    /// </summary>
    public class AllToAnyRewriterPreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        private static readonly MethodInfo EnumerableAnyMethod =
            typeof(Enumerable).GetMethods()
                .FirstOrDefault(m => m.Name == "Any" && m.GetParameters().Length == 2);

        /// <inheritdoc />
        public void Initialize()
        {
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var visited = base.VisitMethodCall(node);

            // Ensure the result is still a method call
            if (visited is MethodCallExpression visitedCall &&
                    visitedCall.Method.Name == "All" &&
                    visitedCall.Arguments.Count == 2)
            {
                var source = visitedCall.Arguments[0];

                if (visitedCall.Arguments[1] is UnaryExpression quote &&
                    quote.Operand is LambdaExpression predicate)
                {
                    // Invert predicate body
                    var notBody = Expression.Not(predicate.Body);
                    var invertedPredicate = Expression.Lambda(notBody, predicate.Parameters);

                    // Get correct generic method
                    var elementType = predicate.Parameters[0].Type;
                    var genericAny = EnumerableAnyMethod?.MakeGenericMethod(elementType)
                        ?? throw new InvalidOperationException("Failed to resolve Enumerable.Any<T>");

                    var anyCall = Expression.Call(genericAny, source, invertedPredicate);

                    // Return !Any(...)
                    return Expression.Not(anyCall);
                }
            }


            return visited;
        }
    }
}
