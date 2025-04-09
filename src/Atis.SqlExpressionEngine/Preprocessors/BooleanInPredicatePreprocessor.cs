using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using Atis.Expressions;
using System.Reflection;
using Atis.SqlExpressionEngine.Exceptions;
using Atis.SqlExpressionEngine.Abstractions;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    public class BooleanInPredicatePreprocessor : ContextAwareExpressionVisitor, IExpressionPreprocessor
    {
        private readonly IBooleanExpressionIdentifier expressionIdentifierPlugin;
        private readonly IReflectionService reflectionService;

        public BooleanInPredicatePreprocessor(IReflectionService reflectionService) : this(null, reflectionService) { }

        public BooleanInPredicatePreprocessor(IBooleanExpressionIdentifier expressionIdentifierPlugin, IReflectionService reflectionService)
        {
            this.expressionIdentifierPlugin = expressionIdentifierPlugin;
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

        /// <inheritdoc />
        protected override bool TryVisit(Expression node, out Expression result)
        {
            if (this.expressionIdentifierPlugin != null)
            {
                if (node.Type == typeof(bool) &&
                    this.expressionIdentifierPlugin.IsMatch(this.GetExpressionStack()) &&
                    IsBooleanPredicateContext())
                {
                    result = CreateEqualToTrueExpression(node);
                    return true;
                }
            }
            return base.TryVisit(node, out result);
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(bool) && IsBooleanPredicateContext())
            {
                return CreateEqualToTrueExpression(node);
            }

            return base.VisitConstant(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Type == typeof(bool) && IsBooleanPredicateContext())
            {
                return CreateEqualToTrueExpression(node);
            }

            return base.VisitMember(node);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce &&
                node.Type == typeof(bool) &&
                this.IsBooleanPredicateContext())
            {
                return CreateEqualToTrueExpression(node);
            }
            return base.VisitBinary(node);
        }

        private Expression CreateEqualToTrueExpression(Expression expression)
        {
            if (expression.Type == typeof(bool?))
                expression = Expression.Coalesce(expression, Expression.Constant(false));
            return Expression.Equal(expression, Expression.Constant(true, typeof(bool)));
        }

        protected virtual bool IsProjectionMethod(MethodInfo method)
        {
            return this.reflectionService.IsProjectionContextMethod(method);
        }

        protected virtual bool IsBooleanPredicateContext()
        {
            try
            {
                var stack = this.GetExpressionStack();

                if (stack.RemainingItems < 2) return false;

                // current will be either Constant, MemberExpression or Coalesce
                // current.Type will be boolean
                var current = stack.Pop();
                var parent = stack.Pop();

                // Rule 1: Inside OR/AND
                if (parent is BinaryExpression binary &&
                    (binary.NodeType == ExpressionType.OrElse || binary.NodeType == ExpressionType.AndAlso))
                    return true;

                // Rule 2: Conditional test
                if (parent is ConditionalExpression conditional && conditional.Test == current)
                    return true;

                if (parent is UnaryExpression notUnary && notUnary.NodeType == ExpressionType.Not)
                    return true;

                // Rule 3: Lambda body but not inside Select/GroupBy/OrderBy
                if (parent is LambdaExpression lambda && lambda.Body == current)
                {
                    var methodLevel = stack.Pop();

                    if (methodLevel is UnaryExpression unary && unary.NodeType == ExpressionType.Quote)
                    {
                        methodLevel = stack.Pop(); // go 1 level above Quote
                    }

                    if (methodLevel is MethodCallExpression methodCall)
                    {
                        if (this.IsProjectionMethod(methodCall.Method))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (StackDepletedException)      // this exception may occur during the Pop call
            {
                return false;
            }
        }
    }

}
