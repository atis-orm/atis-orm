using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Rewrites various forms of Contains/Any equality checks into InValuesExpression.
    ///         Supported patterns:
    ///         1. staticArray.Contains(x.Column)
    ///         2. staticArray.Any(y => y == x.Column)
    ///         3. staticArray.Any(y => x.Column == y)
    ///     </para>
    /// </summary>
    public class InValuesExpressionReplacementPreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        private readonly IReflectionService reflectionService;

        public InValuesExpressionReplacementPreprocessor(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);
        }

        /// <inheritdoc />
        public void Initialize()
        {
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var visited = base.VisitMethodCall(node);

            if (!(visited is MethodCallExpression))
                return visited;

            var methodCall = (MethodCallExpression)visited;

            // Case 1, 2, 3: arr.Contains(x.Column)
            if (methodCall.Method.Name == "Contains" && methodCall.Arguments.Count == 2)
            {
                var arrayExpr = methodCall.Arguments[0];
                var valueExpr = methodCall.Arguments[1];

                if (this.CanEvaluate(arrayExpr))
                {
                    return new InValuesExpression(valueExpr, arrayExpr);
                }
            }

            // Case 4-9: arr.Any(y => y == x.Column) or x.Column == y
            if (methodCall.Method.Name == "Any" && methodCall.Arguments.Count == 2)
            {
                var arrayExpr = methodCall.Arguments[0];
                var lambda = (methodCall.Arguments[1] as UnaryExpression)?.Operand as LambdaExpression
                                    ??
                                    methodCall.Arguments[1] as LambdaExpression;
                if (lambda != null)
                {
                    if (TryExtractEqualityTarget(lambda.Body, lambda.Parameters[0], out var testExpr))
                    {
                        if (this.CanEvaluate(arrayExpr))
                        {
                            return new InValuesExpression(testExpr, arrayExpr);
                        }
                    }
                }
            }

            return visited;
        }

        protected virtual bool CanEvaluate(Expression arrayExpr)
        {
            return this.reflectionService.CanEvaluate(arrayExpr);
        }

        private static bool TryExtractEqualityTarget(Expression body, ParameterExpression param, out Expression target)
        {
            target = null;

            if (body is BinaryExpression binary && binary.NodeType == ExpressionType.Equal)
            {
                if (IsParamReference(binary.Left, param))
                {
                    target = binary.Right;
                    return true;
                }

                if (IsParamReference(binary.Right, param))
                {
                    target = binary.Left;
                    return true;
                }
            }

            return false;
        }

        private static bool IsParamReference(Expression expr, ParameterExpression param)
        {
            return expr is ParameterExpression p && p == param;
        }
    }
}
