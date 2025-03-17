using Atis.Expressions;
using Atis.LinqToSql.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.LinqToSql.Preprocessors
{
    public partial class SpecificationCallRewriterPreprocessor
    {
        /// <summary>
        ///     <para>
        ///         The <c>SpecificationCallRewriterVisitor</c> class is an <see cref="ExpressionVisitor"/>
        ///         that traverses an expression tree and replaces calls to <c>IsSatisfiedBy</c> methods
        ///         in specifications with the actual predicate expressions defined in those specifications.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Caution: this class is not intended to be used by the end user and is not guaranteed
        ///         to be available in future versions.
        ///     </para>
        /// </remarks>
        private class SpecificationCallRewriterVisitor : ExpressionVisitor
        {
            private readonly IReflectionService reflectionService;

            /// <summary>
            ///     Initializes a new instance of the <see cref="SpecificationCallRewriterVisitor"/> class.
            /// </summary>
            /// <param name="reflectionService">The reflection service used for accessing type and member information.</param>
            public SpecificationCallRewriterVisitor(IReflectionService reflectionService)
            {
                this.reflectionService = reflectionService;
            }

            /// <summary>
            ///     Determines whether the specified method call expression represents a call to the
            ///     <c>IsSatisfiedBy</c> method of an <see cref="IExpressionSpecification"/>.
            /// </summary>
            /// <param name="methodCallExpr">The method call expression to check.</param>
            /// <returns><c>true</c> if the method call is to <c>IsSatisfiedBy</c>; otherwise, <c>false</c>.</returns>
            private bool IsSpecificationMethodCall(MethodCallExpression methodCallExpr)
            {
                return
                        typeof(IExpressionSpecification).IsAssignableFrom(methodCallExpr.Method.DeclaringType)
                        &&
                        methodCallExpr.Method.Name == nameof(IExpressionSpecification.IsSatisfiedBy);
            }

            /// <inheritdoc />
            public override Expression Visit(Expression expression)
            {
                var oldExpression = expression;
                if (expression is MethodCallExpression methodCallExpr && methodCallExpr.Object != null && IsSpecificationMethodCall(methodCallExpr))
                {
                    var propertyToConstructorArgMap = new Dictionary<string, Expression>(StringComparer.OrdinalIgnoreCase);
                    object specification;
                    if (methodCallExpr.Object is NewExpression newSpecificationExpr &&
                            newSpecificationExpr.Constructor?.GetParameters().Length > 0)
                    {
                        var ctorParameters = newSpecificationExpr.Constructor?.GetParameters();
                        var ctorArgs = new object[ctorParameters.Length];
                        // Extract public properties once and store them in a case-insensitive HashSet
                        PropertyInfo[] properties = this.reflectionService.GetProperties(newSpecificationExpr.Type);
                        var publicProperties = new HashSet<string>(
                            properties.Select(p => p.Name),
                            StringComparer.OrdinalIgnoreCase // Ensures case-insensitive lookup
                        );
                        for (var i = 0; i < ctorParameters.Length; i++)
                        {
                            var ctorParam = ctorParameters[i];
                            object ctorArg;
                            // Check if a public property matches the constructor argument name
                            if (publicProperties.Contains(ctorParam.Name))
                            {
                                // Store the expression in the map for later replacement
                                propertyToConstructorArgMap[ctorParam.Name] = newSpecificationExpr.Arguments[i];
                                ctorArg = null; // null means we'll be passing null from this parameter in constructor
                            }
                            else // otherwise, it means we didn't find public property matching with constructor argument
                            {
                                try
                                {
                                    // we'll compile the expression mentioned in the constructor argument and assume that it was a constant value
                                    // and pass it as value in below Activator.CreateInstance call
                                    ctorArg = this.reflectionService.Eval(newSpecificationExpr.Arguments[i]);
                                }
                                catch (Exception ex)
                                {
                                    throw new InvalidOperationException($"System was trying to replace Specification's IsSatisfiedBy call with inner expression. But the Specification's constructor call has argument(s) and system is unable to get the value of one argument. Make sure the argument that is passed is either a constant value or the argument name must match (case insensitive) with a public property in specification. Specification = '{newSpecificationExpr.Type}', argument = '{ctorParam.Name}'", ex);
                                }
                            }
                            ctorArgs[i] = ctorArg;
                        }
                        try
                        {
                            // specification = new ExpressionSpecification<T>(param1, param2, ....)
                            specification = this.reflectionService.CreateInstance(newSpecificationExpr.Type, ctorArgs);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Specification was mentioned as 'NewExpression', therefore, system was trying to create the instance of Specification class '{newSpecificationExpr.Type}' using Activator.CreateInstance and mapping individual public properties of Specification class with Constructor arguments but it failed, see inner exception for details.", ex);
                        }
                    }
                    else
                    {
                        try
                        {
                            specification = this.reflectionService.Eval(methodCallExpr.Object);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"System was trying to get the specification instance of type '{methodCallExpr.Object.Type}' by assuming MethodCallExpression.Object is a Constant Value. Make sure that specification instance is a constant expression available in execution context, e.g. `var s = new EntitySpecification(); Expression<Func<Entity, bool>> expr = e => s.IsSatisfiedBy(e)`.", ex);
                        }
                    }
                    IExpressionSpecification typedSpecification = specification as IExpressionSpecification
                                                                    ??
                                                                    throw new InvalidOperationException($"Specification class '{specification.GetType()}' must implement '{nameof(IExpressionSpecification)}' interface.");
                    // get method ToExpression
                    var predicateLambda = typedSpecification.ToExpression()
                                            ??
                                            throw new InvalidOperationException($"Specification class '{specification.GetType()}' {nameof(IExpressionSpecification.ToExpression)} method is returning null.");
                    // recursively using this visitor class so that inner IsSatisfiedBy calls are also replaced
                    predicateLambda = new SpecificationCallRewriterVisitor(this.reflectionService).Visit(predicateLambda) as LambdaExpression
                        ??
                        throw new InvalidOperationException($"System tried to recursively visit the specification's expression to replace inner IsSatisfiedBy calls, but it failed. Make sure that '{nameof(SpecificationCallRewriterVisitor)}' returns a LambdaExpression.");

                    // here we need to replace public properties in predicateExpr with the parameters provided in constructor
                    // also we need to replace the 1st arg of predicateLambda with IsSatisfiedBy method's arg
                    var propertyReplacementVisitor = new SpecificationExpressionRewriterVisitor(predicateLambda, methodCallExpr, specification, propertyToConstructorArgMap);
                    return propertyReplacementVisitor.Visit(predicateLambda.Body);
                }
                return base.Visit(oldExpression);
            }

            /// <summary>
            ///     <para>
            ///         The <c>SpecificationExpressionRewriterVisitor</c> class is an <see cref="ExpressionVisitor"/>
            ///         that replaces parameters and public properties in the predicate expression with the
            ///         corresponding arguments provided in the constructor of the specification.
            ///     </para>
            /// </summary>
            private class SpecificationExpressionRewriterVisitor : ExpressionVisitor
            {
                private readonly LambdaExpression predicateLambda;
                private readonly MethodCallExpression isSatisfiedByCall;
                private readonly object specification;
                private readonly Dictionary<string, Expression> propertyToConstructorArgMap;

                /// <summary>
                ///     Initializes a new instance of the <see cref="SpecificationExpressionRewriterVisitor"/> class.
                /// </summary>
                /// <param name="specificationExpression">The predicate expression of the specification.</param>
                /// <param name="isSatisfiedByCall">The method call expression to <c>IsSatisfiedBy</c>.</param>
                /// <param name="specification">The specification instance.</param>
                /// <param name="propertyToConstructorArgMap">A map of public properties to constructor arguments.</param>
                public SpecificationExpressionRewriterVisitor(LambdaExpression specificationExpression, MethodCallExpression isSatisfiedByCall, object specification, Dictionary<string, Expression> propertyToConstructorArgMap)
                {
                    this.predicateLambda = specificationExpression;
                    this.isSatisfiedByCall = isSatisfiedByCall;
                    this.specification = specification;
                    this.propertyToConstructorArgMap = propertyToConstructorArgMap;
                }

                /// <inheritdoc />
                public override Expression Visit(Expression node)
                {
                    // usually the Specification returns an expression with 1 parameter which is the entity
                    // so we need to replace that parameter with the parameter of IsSatisfiedBy method
                    //      e.g.
                    //              .Where(outerEntity => studentIsAdultSpecification.IsSatisfiedBy(outerEntity.NavStudent()))
                    // Let's assume we are using specification like that in above example, and assume that specification returns
                    // an expression like this,
                    //              student => student.Age > 18
                    // So, above will be replaced by SpecificationCallRewriterVisitor with the expression like this,
                    //              .Where(outerEntity => student.Age > 18)
                    //                                    |_____|
                    //    ___________________________________|
                    //   / 
                    // This (student) is the parameter from the specification's expression, which needs to be replaced with the parameter of IsSatisfiedBy method,
                    // so, in below condition, it is testing that node is matching the first parameter of specification's expression (student) we'll replace it
                    // with IsSatisfiedBy method's parameter which is outerEntity.NavStudent(), so that final expression will look like this,
                    //              .Where(outerEntity => outerEntity.NavStudent().Age > 18)
                    if (node == predicateLambda.Parameters.First())
                        return isSatisfiedByCall.Arguments[0];

                    else if (node is MemberExpression me1
                                && me1.Expression is ConstantExpression ce1 && ce1.Value == specification
                                && propertyToConstructorArgMap.TryGetValue(me1.Member.Name, out var ctorArgExpr))
                    {
                        // here we are testing if we have reached to the point in expression where Public Properties of specification
                        // are being used in the specification's expression                            
                        //                                                        /-------<------------<---------<-----------<--.
                        //                                             __________/______                                        |
                        //                                            |                 |                                       |
                        // e.g. new ScheduleHasNoFlightsSpecification(outerEntity.SomeDay).IsSatisfiedBy(schedule)              ^
                        // Let say above specification returns something like this,                                             |
                        //          schedule => !schedule.NavFlights.Where(flight => flight.FlightDay == this.SomeDay).Any();   |
                        //                                                                               |__________|           |
                        //                          _________________________________________________________/                  |
                        //                     ____/________                                                                    ^
                        //                     |           |                                                                    |
                        //  Below is replacing (this.SomeDay) with (outerEntity.SomeDay) --------->--------->---------->--------`
                        return ctorArgExpr;
                    }
                    return base.Visit(node);
                }
            }
        }
    }
}
