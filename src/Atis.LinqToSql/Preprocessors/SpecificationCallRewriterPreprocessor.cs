using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Atis.LinqToSql.Preprocessors
{

    /// <summary>
    ///     <para>
    ///         The <c>SpecificationCallRewriterPreprocessor</c> class is responsible for preprocessing
    ///         expression trees to replace calls to <c>IsSatisfiedBy</c> methods in specifications
    ///         with the actual predicate expressions defined in those specifications.
    ///     </para>
    /// </summary>
    public partial class SpecificationCallRewriterPreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpecificationCallRewriterPreprocessor"/> class.
        /// </summary>
        /// <param name="reflectionService">The reflection service used for accessing type and member information.</param>
        public SpecificationCallRewriterPreprocessor(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);
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
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var updatedNode = base.VisitMethodCall(node);

            if (updatedNode is MethodCallExpression methodCallExpr && methodCallExpr.Object != null && IsSpecificationMethodCall(methodCallExpr))
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
                                ctorArg = this.reflectionService.Evalulate(newSpecificationExpr.Arguments[i]);
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
                        specification = this.reflectionService.Evalulate(methodCallExpr.Object);
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

                // here we need to replace public properties in predicateExpr with the parameters provided in constructor
                // also we need to replace the 1st arg of predicateLambda with IsSatisfiedBy method's arg
                var propertyReplacementVisitor = new SpecificationExpressionRewriterVisitor(predicateLambda, methodCallExpr, specification, propertyToConstructorArgMap);
                return propertyReplacementVisitor.Visit(predicateLambda.Body);
            }
            return updatedNode;
        }
    }
}
