using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.Internal
{
    /// <summary>
    /// Represents a LINQ query under construction, maintaining a stack of query methods
    /// (e.g., Select, Where, SelectMany) associated with this query.
    /// </summary>
    public class LinqQuery
    {
        /// <summary>
        /// Gets the current query method being processed.
        /// </summary>
        public Expression CurrentQueryMethod { get; private set; }

        /// <summary>
        /// Stack holding all the query methods associated with this query.
        /// The most recent method is at the top of the stack.
        /// </summary>
        public Stack<Expression> MethodStack { get; } = new Stack<Expression>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqQuery"/> class with the initial query method.
        /// </summary>
        /// <param name="queryMethod">The initial query method expression.</param>
        public LinqQuery(Expression queryMethod)
        {
            this.MethodStack.Push(queryMethod);
            this.CurrentQueryMethod = queryMethod;
        }

        /// <summary>
        /// Adds a new query method to the stack and sets it as the current query method.
        /// </summary>
        /// <param name="queryMethod">The query method expression to add.</param>
        public void PushMethod(Expression queryMethod)
        {
            this.MethodStack.Push(queryMethod);
            this.CurrentQueryMethod = queryMethod;
        }

        /// <summary>
        /// Removes the current query method from the stack and updates the current method reference.
        /// </summary>
        public void PopMethod()
        {
            this.MethodStack.Pop();
            this.CurrentQueryMethod = this.MethodStack.Count > 0 ? this.MethodStack.Peek() : null;
        }
    }


    /// <summary>
    /// Manages the lifecycle and nesting of LINQ queries during expression tree traversal.
    /// Tracks query methods and sources, and triggers events on specific actions.
    /// </summary>
    public class LinqQueryContextManager
    {
        /// <summary>
        /// Represents the callback method for query-related events during expression tree traversal.
        /// </summary>
        /// <param name="query">The current active <see cref="LinqQuery"/> instance.</param>
        /// <param name="node">The expression node associated with the event.</param>
        public delegate void QueryEventHandler(LinqQuery query, Expression node);

        private readonly Stack<LinqQuery> queryStack = new Stack<LinqQuery>();
        private readonly IReflectionService reflectionService;
        private readonly Stack<Expression> expressionStack = new Stack<Expression>();
        private LinqQuery _currentQuery;

        /// <summary>
        /// Gets or sets the current active query.
        /// Throws an exception if accessed when no query is active.
        /// </summary>
        public LinqQuery CurrentQuery
        {
            get => this._currentQuery ?? throw new InvalidOperationException($"_currentQuery is null");
            set => this._currentQuery = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqQueryContextManager"/> class.
        /// </summary>
        /// <param name="reflectionService">Reflection service to identify query methods and sources.</param>
        public LinqQueryContextManager(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService ?? throw new ArgumentNullException(nameof(reflectionService));
        }

        /// <summary>
        /// Called when an expression node is about to be visited.
        /// Tracks query methods and manages query stacks.
        /// </summary>
        /// <param name="node">The expression node being visited.</param>
        public void BeforeVisit(Expression node)
        {
            var immediateParent = this.expressionStack.FirstOrDefault();
            if (this.IsQueryMethod(immediateParent))
            {
                this.OnBeforeChildVisit?.Invoke(this.CurrentQuery, node);
            }

            this.expressionStack.Push(node);

            if (this.IsQueryMethod(node))
            {
                var parentNode = expressionStack.Skip(1).FirstOrDefault();
                if (parentNode is null || !this.IsChainedQueryMethod(node, parentNode))
                {
                    // Starting a new query
                    var newQuery = new LinqQuery(node);
                    this.queryStack.Push(newQuery);
                    this.CurrentQuery = newQuery;
                }
                else
                {
                    // Adding a method to an existing query
                    this.CurrentQuery.PushMethod(node);
                }
            }
        }

        private bool IsChainedQueryMethod(Expression currentNode, Expression parentNode)
        {
            return this.reflectionService.IsChainedQueryMethod(currentNode, parentNode);
        }

        /// <summary>
        /// Called after an expression node has been visited.
        /// Triggers events and manages the closure of queries.
        /// </summary>
        /// <param name="node">The expression node just visited.</param>
        public void AfterVisit(Expression node)
        {
            if (this.IsQuerySource(node))
            {
                this.OnQuerySourceCreated?.Invoke(this.CurrentQuery, node);
            }

            if (this.CurrentQuery.CurrentQueryMethod == node)
            {
                this.CurrentQuery.PopMethod();

                if (this.CurrentQuery.MethodStack.Count == 0)
                {
                    // Query is fully processed
                    this.OnQueryClosed?.Invoke(this.CurrentQuery, node);
                    this.queryStack.Pop();
                    this._currentQuery = this.queryStack.Count > 0 ? this.queryStack.Peek() : null;
                }
                else
                {
                    this.OnQueryMethodExit?.Invoke(this.CurrentQuery, node);
                }
            }

            this.expressionStack.Pop();
        }

        /// <summary>
        /// Event triggered when a query source (e.g., table, collection) is identified.
        /// </summary>
        public QueryEventHandler OnQuerySourceCreated { get; set; }

        /// <summary>
        /// Event triggered when a query is fully processed and closed.
        /// </summary>
        public QueryEventHandler OnQueryClosed { get; set; }

        /// <summary>
        /// Event triggered when exiting from a query method (e.g., Select, Where).
        /// </summary>
        public QueryEventHandler OnQueryMethodExit { get; set; }

        /// <summary>
        /// Event triggered before visiting a child expression within a query method.
        /// </summary>
        public QueryEventHandler OnBeforeChildVisit { get; set; }

        private bool IsQuerySource(Expression node) => this.reflectionService.IsQuerySource(node);
        private bool IsQueryMethod(Expression node) => this.reflectionService.IsQueryMethod(node);
    }


}
