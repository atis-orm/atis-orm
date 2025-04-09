using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    public interface IBooleanExpressionIdentifier
    {
        bool IsMatch(ArrayStack expressionStack);
    }

    public class BinaryToTernaryInProjectionPreprocessor : ContextAwareExpressionVisitor, IExpressionPreprocessor
    {
        private readonly IReflectionService reflectionService;
        private IBooleanExpressionIdentifier binaryExpressionIdentifierPlugin;

        public BinaryToTernaryInProjectionPreprocessor(IReflectionService reflectionService) : this(null, reflectionService) { }

        public BinaryToTernaryInProjectionPreprocessor(IBooleanExpressionIdentifier binaryExpressionIdentifierPlugin, IReflectionService reflectionService)
        {
            this.binaryExpressionIdentifierPlugin = binaryExpressionIdentifierPlugin;
            this.reflectionService = reflectionService;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression expression)
        {
            return this.Visit(expression);
        }

        protected override bool TryVisit(Expression node, out Expression result)
        {
            if (this.binaryExpressionIdentifierPlugin != null)
            {
                if ((node.Type == typeof(bool) || node.Type == typeof(bool?)) &&
                    this.binaryExpressionIdentifierPlugin.IsMatch(this.GetExpressionStack()))
                {
                    result = Expression.Condition(node, Expression.Constant(true), Expression.Constant(false));
                    return true;
                }
            }
            return base.TryVisit(node, out result);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var visited = base.VisitBinary(node);

            if (visited is BinaryExpression &&
                visited.Type == typeof(bool) &&
                // since Coalesce itself is a Projection function which returns a scalar value
                // therefore we'll never wrap it in case when, however, if any part of
                // of Coalesce is a comparison expression then it is already handled in
                // nested VisitBinary call
                visited.NodeType != ExpressionType.Coalesce &&
                IsInProjectionContext())
            {
                return Expression.Condition(visited, Expression.Constant(true), Expression.Constant(false));
            }

            return visited;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var visited = base.VisitUnary(node);

            if (visited is UnaryExpression unaryExpression &&
                visited.NodeType == ExpressionType.Not &&
                (visited.Type == typeof(bool) || visited.Type == typeof(bool?)) &&
                IsInProjectionContext())
            {
                bool castToNullableBool = false;
                // e.g. .Select(x => new { IsAllowed = x.IsValid && !x.Allowed });
                //  translates to
                //          select a_1.IsValid and case when a_1.Allowed then false else true end
                if (visited.Type == typeof(bool?))
                {
                    visited = Expression.Coalesce(unaryExpression.Operand, Expression.Constant(false));
                    castToNullableBool = true;
                }
                if (castToNullableBool)
                    visited = Expression.Condition(visited, Expression.Constant(false), Expression.Constant(true));
                else
                    visited = Expression.Condition(visited, Expression.Constant(true), Expression.Constant(false));
                if (castToNullableBool)
                    visited = Expression.Convert(visited, typeof(bool?));
            }
            return visited;
        }

        protected virtual bool IsProjectionMethod(MethodInfo method)
        {
            return this.reflectionService.IsProjectionContextMethod(method);
        }

        protected virtual bool IsInProjectionContext()
        {
            try
            {
                var stack = this.GetExpressionStack();

                if (stack.RemainingItems < 2) return false;

                // current will be BinaryExpression but not Coalesce
                // and current.Type will always be boolean
                // as this is being tested when this method is being called
                var current = stack.Pop();
                var parent = stack.Pop();

                // Case 1: inside true/false part of a ternary
                if (parent is ConditionalExpression ce &&
                    (ce.IfTrue == current || ce.IfFalse == current))
                    return true;

                if (parent is BinaryExpression be &&
                    be.NodeType == ExpressionType.Coalesce)
                {
                    if (be.Left == current || be.Right == current)
                        // e.g. .Select(x => new { IsAllowed = x.IsValid && (x.Allowed ?? (x.AnotherFlag ?? x.Age > 18)) });
                        //  translates to
                        //          select a_1.IsValid and isNull(a_1.Allowed, isNull(x.AnotherFlag, a_1.Age > 18))
                        // so we need to do the 'case when' in the outer most and in the a_1.Age > 18 part
                        return true;
                    else
                        // otherwise it means you are on the Test part so it's ok to have
                        // binary expression (boolean) there
                        return false;
                }

                if (parent is NewExpression || parent is MemberInitExpression || ((parent as UnaryExpression)?.NodeType == ExpressionType.Convert))
                    return true;

                // Case 2: inside the body of a lambda used in projection methods
                if (parent is LambdaExpression lambda && lambda.Body == current)
                {
                    var methodLevel = stack.Pop();

                    if (methodLevel is UnaryExpression unary && unary.NodeType == ExpressionType.Quote)
                    {
                        methodLevel = stack.Pop();
                    }

                    if (methodLevel is MethodCallExpression methodCall)
                    {
                        if (this.IsProjectionMethod(methodCall.Method))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (StackDepletedException)
            {
                return false;
            }
        }
    }
}
