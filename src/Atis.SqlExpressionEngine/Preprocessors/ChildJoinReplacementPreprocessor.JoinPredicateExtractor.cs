using Atis.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    public partial class ChildJoinReplacementPreprocessor
    {
        /// <summary>
        ///     <para>
        ///         This class is used to extract the join predicates from the given expression.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Caution: this class is not intended to be used by the end user and is not guaranteed
        ///         to be available in future versions.
        ///     </para>
        /// </remarks>
        private class JoinPredicateExtractor : ExpressionVisitor
        {
            private bool firstIteration = true;
            /// <summary>
            ///     <para>
            ///         Gets the flag that indicates if the join is not possible.
            ///     </para>
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         This flag is set to <c>true</c> if the join is not possible, so that caller
            ///         can decide what to do with the query expression.
            ///     </para>
            ///     <para>
            ///         Usually this flag is set to <c>true</c> if the join predicate is used between <c>OrElse</c> comparison or
            ///         query is not ending with <c>Where</c> call.
            ///     </para>
            ///     <para>
            ///         Also if the <c>sourceParameter</c> is used in other than <c>Where</c> call than this flag is set to <c>true</c>.
            ///     </para>
            /// </remarks>
            public bool JoinIsNotPossible { get; private set; }

            private readonly ParameterExpression sourceParameter;
            private readonly ParameterExpression childTableParameter;
            private readonly List<BinaryExpression> joinPredicates = new List<BinaryExpression>();
            public IReadOnlyCollection<BinaryExpression> JoinPredicates => this.joinPredicates;

            /// <summary>
            ///     <para>
            ///         Creates new instance of <see cref="JoinPredicateExtractor"/>.
            ///     </para>
            /// </summary>
            /// <param name="sourceParameter">Parameter of parent query expression.</param>
            /// <param name="childTableParameter">Parameter of Lambda Expression used in 3rd argument of <c>ChildJoin</c>.</param>
            public JoinPredicateExtractor(ParameterExpression sourceParameter, ParameterExpression childTableParameter)
            {
                this.sourceParameter = sourceParameter;
                this.childTableParameter = childTableParameter;
            }

            /// <inheritdoc />
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // node will be a Query expression, e.g. q.Where(....)
                if (this.firstIteration)
                {
                    if (node.Method.Name != nameof(Queryable.Where))
                    {
                        this.JoinIsNotPossible = true;
                    }
                    this.firstIteration = false;
                }
                var joinIsPossible = !this.JoinIsNotPossible;
                if (joinIsPossible)
                {
                    if (node.Method.Name == nameof(Queryable.Where) && node.Arguments.Count == 2)
                    {
                        var unaryExpression = node.Arguments[1] as UnaryExpression;
                        var predicateLambda = unaryExpression?.Operand as LambdaExpression
                                                ??
                                                node.Arguments[1] as LambdaExpression;
                        if (predicateLambda != null && predicateLambda.Parameters.Count == 1)
                        {
                            var predicateExpressionExtractor = new PredicateExpressionExtractor(this.sourceParameter);
                            var updatedPredicateBody = predicateExpressionExtractor.Visit(predicateLambda.Body);
                            if (predicateExpressionExtractor.HasOrElseComparison)
                            {
                                this.JoinIsNotPossible = true;
                            }
                            else if (predicateExpressionExtractor.PredicateExpressions.Count > 0)
                            {
                                var param0 = predicateLambda.Parameters.First();
                                // the extracted predicate expressions will have Where clause's parameter, we need to replace it with childTableParameter
                                // so that predicate expression can be used in ChildJoinExpression.
                                foreach (var predicateExpression in predicateExpressionExtractor.PredicateExpressions)
                                {
                                    var newPredicate = ExpressionReplacementVisitor.Replace(param0, this.childTableParameter, predicateExpression) as BinaryExpression
                                                        ??
                                                        throw new InvalidOperationException("The comparison expression is not a binary expression.");
                                    this.joinPredicates.Add(newPredicate);
                                }
                                Expression callArg1 = Expression.Lambda(updatedPredicateBody, param0);
                                if (unaryExpression != null)
                                    callArg1 = Expression.Quote(callArg1);
                                var callArg0 = this.Visit(node.Arguments[0]);
                                // creating new Where method call which has all the join predicate expressions converted to "true"
                                return Expression.Call(null, node.Method, new[] { callArg0, callArg1 });
                            }
                        }
                    }
                    else
                    {
                        // if we are here then it means method call is NOT `Where` call so
                        // we'll check if the sourceParameter is being used anywhere in this method call
                        // then we cannot convert this query expression to ChildJoin.
                        var expressionFinder = new ExpressionFinder(this.sourceParameter);
                        if (expressionFinder.FindIn(node))
                        {
                            this.JoinIsNotPossible = true;
                        }
                    }
                }
                return base.VisitMethodCall(node);
            }

            /// <summary>
            ///     <para>
            ///         Extracts the predicate expressions from the given expression.
            ///     </para>
            /// </summary>
            private class PredicateExpressionExtractor : ExpressionVisitor
            {
                private readonly Stack<Expression> stack = new Stack<Expression>();

                /// <summary>
                ///     <para>
                ///         Creates a new instance of <see cref="PredicateExpressionExtractor"/>.
                ///     </para>
                /// </summary>
                /// <remarks>
                ///     <para>
                ///         Checks the <c>BinaryExpressions</c> of type Equal, Not Equal, Greater Than, Greater Than or Equal, Less Than, Less Than or Equal
                ///         and checks if <paramref name="sourceParameter"/> is used in them, if it does, then it considers
                ///         them as join predicates, and removes from the given expression and adds them to <see cref="PredicateExpressions"/>.
                ///     </para>
                /// </remarks>
                /// <param name="sourceParameter">Source parameter of the query.</param>
                public PredicateExpressionExtractor(ParameterExpression sourceParameter)
                {
                    this.SourceParameter = sourceParameter ?? throw new ArgumentNullException(nameof(sourceParameter));
                }

                /// <summary>
                ///     <para>
                ///         Gets source parameter that was provided in the constructor.
                ///     </para>
                /// </summary>
                public ParameterExpression SourceParameter { get; }

                private readonly List<BinaryExpression> predicateExpressions = new List<BinaryExpression>();
                /// <summary>
                ///     <para>
                ///         Gets the extracted predicate expressions.
                ///     </para>
                /// </summary>
                public IReadOnlyCollection<BinaryExpression> PredicateExpressions => this.predicateExpressions;
                /// <summary>
                ///     <para>
                ///         Gets the flag that indicates if the join predicate is being used in OrElse comparison.
                ///     </para>
                /// </summary>
                /// <remarks>
                ///     <para>
                ///         If this flag is <c>true</c> then query expression should not be converted to
                ///         <c>ChildJoinExpression</c>.
                ///     </para>
                ///     <para>
                ///         Parameter should check this flag after <c>Visit</c> call is finished.
                ///     </para>
                /// </remarks>
                public bool HasOrElseComparison { get; private set; }

                /// <inheritdoc/> />
                public override Expression Visit(Expression node)
                {
                    if (node is null)
                        return node;

                    this.stack.Push(node);
                    var result = base.Visit(node);
                    this.stack.Pop();
                    return result;
                }

                /// <inheritdoc />
                protected override Expression VisitBinary(BinaryExpression node)
                {
                    var result = base.VisitBinary(node);
                    switch (result.NodeType)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.LessThanOrEqual:
                        case ExpressionType.LessThan:
                            var hasSourceParameterInLeft = this.HasSourceParameter(node.Left);
                            var hasSourceParameterInRight = this.HasSourceParameter(node.Right);
                            if (hasSourceParameterInLeft || hasSourceParameterInRight)
                            {
                                if (!this.CheckOrElseComparison())
                                {
                                    this.predicateExpressions.Add(node);
                                    return Expression.Constant(true);
                                }
                            }
                            break;
                    }
                    return result;
                }

                /// <summary>
                ///     <para>
                ///         Checks in the current <see cref="stack"/> if there is any 
                ///         <see cref="BinaryExpression"/> of type <see cref="ExpressionType.OrElse"/>.
                ///     </para>
                /// </summary>
                /// <returns><c>true</c> if there is any <see cref="BinaryExpression"/> of type <see cref="ExpressionType.OrElse"/>; otherwise, <c>false</c>.</returns>
                private bool CheckOrElseComparison()
                {
                    if (!this.HasOrElseComparison)
                    {
                        var stackToArray = this.stack.ToArray();
                        for (var i = 0; i < stackToArray.Length; i++)
                        {
                            if (stackToArray[i] is BinaryExpression binaryExpression)
                            {
                                if (binaryExpression.NodeType == ExpressionType.OrElse)
                                {
                                    this.HasOrElseComparison = true;
                                    break;
                                }
                            }
                        }
                    }
                    return this.HasOrElseComparison;
                }

                /// <summary>
                ///     <para>
                ///         Searches the <see cref="SourceParameter"/> in <paramref name="node"/>.
                ///     </para>
                /// </summary>
                /// <param name="node">Expression to search in.</param>
                /// <returns><c>true</c> if <see cref="SourceParameter"/> is found in <paramref name="node"/>; otherwise, <c>false</c>.</returns>
                private bool HasSourceParameter(Expression node)
                {
                    var expressionFinder = new ExpressionFinder(this.SourceParameter);
                    return expressionFinder.FindIn(node);
                }
            }
        }
    }

}
