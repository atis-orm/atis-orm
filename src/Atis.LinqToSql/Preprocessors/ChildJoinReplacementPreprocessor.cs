using Atis.Expressions;
using Atis.LinqToSql.ExpressionExtensions;
using Atis.LinqToSql.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Looks for <c>SelectMany</c> method call and replaces the 2nd argument with
    ///         <see cref="ChildJoinExpression"/> expression.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Usually the <c>SelectMany</c> call is translated to <c>Cross Join</c> or <c>Cross Apply</c>, however,
    ///         if the provided query in 2nd argument of <c>SelectMany</c> has a <c>Where</c> call then
    ///         this class checks if the <c>Where</c> call has a join condition and if it has then
    ///         it replaces the whole query expression with <see cref="ChildJoinExpression"/> expression.
    ///         Which in turn, translates the result to <c>Inner Join</c>.
    ///     </para>
    ///     <para>
    ///         The <see cref="ChildJoinExpression"/> expression is then converted to inner join or left join
    ///         later in the conversion process.
    ///     </para>
    /// </remarks>
    public partial class ChildJoinReplacementPreprocessor : IExpressionPreprocessor
    {
        private readonly IReflectionService reflectionService;

        
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildJoinReplacementPreprocessor"/> class.
        /// </summary>
        /// <param name="reflectionService">The reflection service used for various reflection operations.</param>
        public ChildJoinReplacementPreprocessor(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }

        /// <inheritdoc/>
        public Expression Preprocess(Expression node, Expression[] expressionsStack)
        {
            if (node is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.Name == nameof(Queryable.SelectMany) &&
                methodCallExpression.Arguments.Count >= 2)
            {
                return this.VisitMethodCall(methodCallExpression);
            }
            return node;
        }

        private Expression VisitMethodCall(MethodCallExpression node)
        {
            // e.g. arg1 can be
            //      Quote (        e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" && x.RowId == e.RowId)        )
            var arg1 = node.Arguments[1];
            var unaryExpression = arg1 as UnaryExpression;
            var arg1Lambda = unaryExpression?.Operand as LambdaExpression
                             ??
                             arg1 as LambdaExpression;
            if (arg1Lambda != null)
            {
                var childJoin = this.CreateChildJoin(arg1Lambda);
                if (childJoin != null)
                {
                    // If we are here then it means query expression can be replaced with ChildJoinExpression.
                    var selectManyArg0 = node.Arguments[0]; 
                    // IMPORTANT:   here we are using arg1Lambda.Type, this is because we noticed that
                    //              the Lambda part of the expression had IEnumerable<T> type while the body had IQueryable<T> type
                    //              that's why we are picking the Lambda's own type, instead of childJoin.Type.
                    var selectManyArg1 = Expression.Lambda(arg1Lambda.Type, childJoin, arg1Lambda.Parameters[0]);       // enclosing ChildJoin call into Lambda
                    var otherSelectArgs = node.Arguments.Skip(2).ToArray();
                    IEnumerable<Expression> finalList = new[] { selectManyArg0, selectManyArg1 };                       // creating final SelectMany arguments
                    if (otherSelectArgs.Length > 0)
                        finalList = finalList.Concat(otherSelectArgs);
                    return Expression.Call(null, node.Method, finalList);                                               // creating new SelectMany call
                }
            }

            return node;
        }

        private Expression CreateChildJoin(LambdaExpression selectManyArg1Lambda)
        {
            // selectManyArg1Lambda is a query, note that if selectManyArg1Lambda has already been replaced by ChildJoinExpression below condition
            // will still be true, so it seems like that we might be replacing again and again, but this is 
            // not the case
            if (this.reflectionService.IsQueryMethod(selectManyArg1Lambda.Body))      // IQueryable<T>
            {
                // because here we are checking that the query must have a "Where" method call, so, in case of ChildJoin there
                // won't be a Where call.
                // Only if the query expression is ending on "Where" then we'll try to convert it to ChildJoin
                //      E.g.: q.SelectMany(p => childTable.Where(c => c.FK == p.PK))
                //                        |________________________________________|
                //                                           |
                //                                  selectManyArg1Lambda


                var (methodCallExpr, defaultIfEmpty, navigationName) = this.GetInternalMethodCallFromSelectManyArg1Lambda(selectManyArg1Lambda);
                if (methodCallExpr == null || methodCallExpr.Method.Name != nameof(Queryable.Where))
                {
                    return null;
                }

                if (navigationName == null)
                    navigationName = "child_join";
                                                                                                                        // extracting the child table's type
                var childTableType = this.reflectionService.GetEntityTypeFromQueryableType(selectManyArg1Lambda.Body.Type)
                                        ??
                                        throw new InvalidOperationException($"Unable to determine the entity type from type {selectManyArg1Lambda.Body.Type}.");
                var sourceParam = selectManyArg1Lambda.Parameters[0];                                                   // this will be used as the Source in ChildJoinExpression
                var childTableParameter = Expression.Parameter(childTableType);                                         // parameter to use in join condition of ChildJoinExpression
                var joinPredicateExtractor = new JoinPredicateExtractor(sourceParam, childTableParameter);
                var updatedMethodCallExpr = joinPredicateExtractor.Visit(methodCallExpr);                               // extracting the Join Predicate
                // Below if condition is checking that if above predicate extractor extracted any join predicates
                //      and also
                //  JoinPredicateExtractor suggests that join is possible (see JoinIsNotPossible property for the details)
                if (!joinPredicateExtractor.JoinIsNotPossible && joinPredicateExtractor.JoinPredicates.Count > 0)
                {
                    var redundantTrueRemover = new RemoveRedundantTrueVisitor();
                    // joinPredicateExtractor not only extracts the predicates but also removes the predicate expressions
                    // from the original expression, and replaces them by "true", so we need to remove all those "true"
                    // from expression.
                    updatedMethodCallExpr = redundantTrueRemover.Visit(updatedMethodCallExpr);

                    // Create ChildJoinExpression
                    Expression parent = sourceParam;
                    Expression childSource = updatedMethodCallExpr;
                    BinaryExpression childJoinArg2Body = CombineJoinPredicates(joinPredicateExtractor.JoinPredicates);
                    LambdaExpression joinCondition = Expression.Lambda(childJoinArg2Body, childTableParameter);
                    Expression childJoinCall = new ChildJoinExpression(parent, childSource, joinCondition, NavigationType.ToChildren, navigationName);
                    if (defaultIfEmpty)
                    {
                        childJoinCall = Expression.Call(typeof(Queryable), nameof(Queryable.DefaultIfEmpty), new[] { childTableType }, childJoinCall);
                    }
                    return childJoinCall;
                }
            }
            return null;
        }

        private (MethodCallExpression queryMethod, bool defaultIfEmpty, string navigationName) GetInternalMethodCallFromSelectManyArg1Lambda(LambdaExpression selectManyArg1Lambda)
        {
            string navigationName = null;
            bool defaultIfEmpty = false;
            /*
             * selectManyArg1Lambda.Body can be
             *      1. SubQueryNavigationExpression
             *          in this case we'll pick the Query from SubQueryNavigationExpression and check if it's a method call
             *      2. MethodCallExpression
             *          in this case either it's a Where call directly or it's a DefaultIfEmpty call
             *          2.1. in-case of DefaultIfEmpty it might have again internally SubQueryNavigationExpression
             */

            //      employee.Degrees.Where(x => x.Degree == "123");
            //  transforms to
            //      SubQuery(DataSet<Degree>().Where(d => d.EmployeeId == employee.EmployeeId), "Degrees").Where(x => x.Degree == "123")
            //
            //
            //      employee.Degrees                            (used in join)
            //  transforms to
            //      SubQuery(DataSet<Degree>().Where(d => d.EmployeeId == employee.EmployeeId), "Degrees")          <---------.
            //                                                                                                                |
            //                                                                                                                |
            //      employee.Degrees.DefaultIfEmployee()        (used in join)                                                |
            //  transforms to                                                                                                 |
            //      SubQuery(DataSet<Degree>().Where(d => d.EmployeeId == employee.EmployeeId), "Degrees").DefaultIfEmpty()   |      <--------.
            //                                                                                                                |               |
            //                                                                                                             ___|               |
            MethodCallExpression methodCallExpression;                                                      //            /                   |
            if (selectManyArg1Lambda.Body is SubQueryNavigationExpression subQueryNavigationExpression)     //  true in this case             |
            {                                                                                               //                                | 
                methodCallExpression = subQueryNavigationExpression.Query as MethodCallExpression;          //                                |
                navigationName = subQueryNavigationExpression.NavigationProperty;                           //                                |
            }                                                                                               //                                |
            else                                                                                            //                                |
                methodCallExpression = selectManyArg1Lambda.Body as MethodCallExpression;                   //                                |
                                                                                                            //                                |
            if (methodCallExpression != null)                                                               //                                |
            {                                                                                               //                                |
                if (methodCallExpression.Method.Name == nameof(Queryable.DefaultIfEmpty))                   //                                |
                {                                                                                           //                                |
                    var firstArg = methodCallExpression.Arguments[0];                                       //                                |
                    if (firstArg is SubQueryNavigationExpression subQueryNavigationExpression2)             // will be true in this case -----`
                    {
                        methodCallExpression = subQueryNavigationExpression2.Query as MethodCallExpression;
                        navigationName = subQueryNavigationExpression2.NavigationProperty;
                    }
                    else
                        methodCallExpression = firstArg as MethodCallExpression;

                    defaultIfEmpty = true;
                }
            }
            return (methodCallExpression, defaultIfEmpty, navigationName);
        }

        private BinaryExpression CombineJoinPredicates(IEnumerable<BinaryExpression> joinPredicates)
        {
            // this method combines the given join predicates into AndAlso expression
            BinaryExpression childJoinArg2Body = joinPredicates.First();
            foreach (var joinComparison in joinPredicates.Skip(1))
            {
                childJoinArg2Body = Expression.AndAlso(childJoinArg2Body, joinComparison);
            }
            return childJoinArg2Body;
        }

        /// <inheritdoc />
        public void BeforeVisit(Expression node, Expression[] expressionsStack)
        {
            // do nothing
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }
    }

}
