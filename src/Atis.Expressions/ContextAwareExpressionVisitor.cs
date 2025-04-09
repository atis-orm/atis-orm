using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{
    public class ArrayStack
    {
        public Expression[] StackArray { get; }
        public int CurrentIndex { get; private set; }
        public ArrayStack(Stack<Expression> stack)
        {
            this.StackArray = stack.ToArray();
            this.CurrentIndex = 0;
        }
        public int RemainingItems => this.StackArray.Length - this.CurrentIndex;
        public Expression Pop()
        {
            if (this.CurrentIndex >= this.StackArray.Length)
            {
                throw new StackDepletedException();
            }
            return this.StackArray[this.CurrentIndex++];
        }
    }

    public class ContextAwareExpressionVisitor : ExpressionVisitor
    {
        private readonly Stack<Expression> expressionStack = new Stack<Expression>();
    
        /// <inheritdoc />
        public sealed override Expression Visit(Expression node)
        {
            if (node == null) return null;

            this.expressionStack.Push(node);
            try
            {
                if (this.TryVisit(node, out var result))
                {
                    return result;
                }
                return base.Visit(node);
            }
            finally
            {
                this.expressionStack.Pop();
            }
        }

        /// <summary>
        ///     <para>
        ///         This method is called when the visitor is about to visit a node.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Since the main <c>Visit</c> method of this class is sealed and cannot be overridden. 
        ///         Instead, user can override this method to implement custom logic that is required in
        ///         <c>Visit</c> method. This method is called right before <c>base.Visit</c> call.
        ///     </para>
        ///     <para>
        ///         If this method returns <c>true</c> and <paramref name="result"/> is set then base 
        ///         class will not perform any further nested visit and will simply return the <paramref name="result"/>.
        ///     </para>
        /// </remarks>
        /// <param name="node">Expression to visit.</param>
        /// <param name="result">Resulting expression after visiting.</param>
        /// <returns>Return <c>true</c> if the node was visited and <c>false</c> otherwise.</returns>
        protected virtual bool TryVisit(Expression node, out Expression result)
        {
            result = null;
            return false;
        }

        protected ArrayStack GetExpressionStack()
        {
            return new ArrayStack(this.expressionStack);
        }
    }
}
