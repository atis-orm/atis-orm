using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using Atis.Expressions;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    public interface IBooleanExpressionIdentifier
    {
        bool IsBooleanExpression(Expression expression);
    }

    public class BooleanToEqualRewritePreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        private readonly Stack<Expression> parentExpressionStack = new Stack<Expression>();
        private readonly IBooleanExpressionIdentifier expressionIdentifierPlugin;

        public BooleanToEqualRewritePreprocessor() : this(null) { }

        public BooleanToEqualRewritePreprocessor(IBooleanExpressionIdentifier expressionIdentifierPlugin)
        {
            this.expressionIdentifierPlugin = expressionIdentifierPlugin;
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
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;

            parentExpressionStack.Push(node);
            try
            {
                if (this.expressionIdentifierPlugin != null)
                {
                    if (IsBooleanPredicateContext() &&
                        (node.Type == typeof(bool) || node.Type == typeof(bool?)) &&
                        this.expressionIdentifierPlugin.IsBooleanExpression(node))
                    {
                        return CreateEqualToTreeExpression(node);
                    }
                }
                var result = base.Visit(node);
                return result;
            }
            finally
            {
                parentExpressionStack.Pop();
            }
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (IsBooleanPredicateContext() &&
                (node.Type == typeof(bool) || node.Type == typeof(bool?)))
            {
                return CreateEqualToTreeExpression(node);
            }

            return base.VisitConstant(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            if (IsBooleanPredicateContext() &&
                (node.Type == typeof(bool) || node.Type == typeof(bool?)))
            {
                return CreateEqualToTreeExpression(node);
            }

            return base.VisitMember(node);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce
                    &&
                this.IsBooleanPredicateContext())
            {
                return CreateEqualToTreeExpression(node);
            }
            return base.VisitBinary(node);
        }

        private Expression CreateEqualToTreeExpression(Expression expression)
        {
            if (expression.Type == typeof(bool?))
                expression = Expression.Coalesce(expression, Expression.Constant(false));
            return Expression.Equal(expression, Expression.Constant(true, typeof(bool)));
        }

        private class NoMoreElementsInStackException : Exception
        {
            public NoMoreElementsInStackException() : base("No more elements in the stack.") { }
        }

        private Expression PopValueFromStack(Expression[] stack, ref int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            if (index >= stack.Length)
                throw new NoMoreElementsInStackException();
            var value = stack[index];
            index++;
            return value;
        }

        private bool IsBooleanPredicateContext()
        {
            try
            {
                var copy = this.parentExpressionStack.ToArray();

                if (copy.Length < 2) return false;

                var currentIndex = 0;

                var current = this.PopValueFromStack(copy, ref currentIndex);
                var parent = this.PopValueFromStack(copy, ref currentIndex);

                // Rule 1: Inside OR/AND
                if (parent is BinaryExpression binary &&
                    (binary.NodeType == ExpressionType.OrElse || binary.NodeType == ExpressionType.AndAlso))
                    return true;

                // Rule 2: Conditional test
                if (parent is ConditionalExpression conditional && conditional.Test == current)
                    return true;

                // Rule 3: Lambda body but not inside Select/GroupBy/OrderBy
                if (parent is LambdaExpression lambda && lambda.Body == current)
                {
                    var methodLevel = this.PopValueFromStack(copy, ref currentIndex);

                    if (methodLevel is UnaryExpression unary && unary.NodeType == ExpressionType.Quote)
                    {
                        methodLevel = this.PopValueFromStack(copy, ref currentIndex); // unwrap quote
                    }

                    if (methodLevel is MethodCallExpression methodCall)
                    {
                        var methodName = methodCall.Method.Name;
                        if (methodName == nameof(Queryable.Select) || methodName == nameof(Queryable.OrderBy) ||
                            methodName == nameof(Queryable.OrderByDescending) || methodName == nameof(Queryable.ThenBy) ||
                            methodName == nameof(Queryable.ThenByDescending) || methodName == nameof(Queryable.GroupBy) ||
                            methodName == nameof(QueryExtensions.OrderByDesc))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (NoMoreElementsInStackException)
            {
                return false;
            }
        }
    }

}
