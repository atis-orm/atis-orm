using Atis.LinqToSql.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Atis.LinqToSql.Preprocessors
{
    public partial class QueryMethodGenericTypeReplacementPreprocessor
    {
        private class FixLinqMethodCallTSource
        {
            private readonly IReflectionService reflectionService;

            public FixLinqMethodCallTSource(IReflectionService reflectionService)
            {
                this.reflectionService = reflectionService;
            }

            public MethodCallExpression Transform(MethodCallExpression node)
            {
                if (this.IsQueryMethod(node))
                {
                    var firstArg = node.Arguments.FirstOrDefault(); // IQueryable<TSource>
                    if (firstArg != null)
                    {
                        // this will be actual type, for example,
                        //      Where<IModelWithItem>(queryOnInterface, x => x.NavItem().ItemId == 1)
                        // queryOnInterface is actually IQueryable<IModelWithItem> but below method will pick
                        // the underlying object's type so it will be IQueryable<Asset> and thus
                        // firstArgQueryableType will be Asset
                        var firstArgQueryableType = this.GetEnumerableType(firstArg.Type);

                        if (firstArgQueryableType != null)
                        {
                            var firstParameterType = node.Method.GetParameters().First().ParameterType;                 // IQueryable<IModelWithItem>
                            var firstParameterQueryableType = this.GetEnumerableType(firstParameterType);               // IModelWithItem

                            if (firstParameterQueryableType != null && firstParameterQueryableType != firstArgQueryableType)
                            {
                                // if we are here it means the first parameter of the query method is not matching with the
                                // generic first type of the method call, which will usually happen when the 1st argument
                                // is of interface or base class type and the actual object is of derived class type
                                // e.g. Where<IModelWithItem>(queryOnInterface, x => x.NavItem().ItemId == 1)
                                // in above example, Where's 1st parameter type is IQueryable<IModelWithItem> but the 
                                // variable queryOnInterface is actually pointing to an object of type IQueryable<Asset>,
                                // so the method's `argument` type is different then the method's `parameter` type
                                // argument = object that is being passed
                                // parameter type = type defined on the method's parameter
                                // e.g. a method can except IEnumerable as parameter and we can pass a List, Array, etc.
                                // so method's parameter type will be IEnumerable, but to know exactly what is being passed
                                // we will be looking at the argument type.

                                var methodGenericArgs = node.Method.GetGenericArguments();

                                // Select<IModelWithItem, IModelWithItem>  =>   Select<Asset, Asset>
                                // Select<IModelWithItem, Anonymous> => Select<Asset, Anonymous>
                                Type[] updatedArgumentTypes = methodGenericArgs
                                                                    .Select(t => t == firstParameterQueryableType ? firstArgQueryableType : t)
                                                                    .ToArray();

                                // Get the corrected method
                                MethodInfo correctedMethod = node.Method.GetGenericMethodDefinition()
                                    .MakeGenericMethod(updatedArgumentTypes);

                                // Update lambda parameter types in the arguments
                                var newArguments = node.Arguments.ToArray();
                                for (int i = 1; i < newArguments.Length; i++)
                                {
                                    if (newArguments[i] is UnaryExpression unary && unary.Operand is LambdaExpression lambda)
                                    {
                                        // Update only parameters that match the original TSource type
                                        var newParams = lambda.Parameters.Select(p =>
                                            p.Type == firstParameterQueryableType
                                                ? Expression.Parameter(firstArgQueryableType, p.Name) // Change only if it matches TSource
                                                : p
                                        ).ToArray();

                                        var newBody = new TypeReplacer(lambda.Parameters, newParams).Visit(lambda.Body);
                                        var newLambda = Expression.Lambda(newBody, newParams);

                                        newArguments[i] = Expression.Quote(newLambda);
                                    }
                                }

                                return Expression.Call(correctedMethod, newArguments);
                            }
                        }
                    }
                }

                return node;
            }

            private bool IsQueryMethod(MethodCallExpression node)
            {
                return this.reflectionService.IsQueryMethod(node);
            }

            private Type GetEnumerableType(Type queryableType)
            {
                return this.reflectionService.GetEntityTypeFromQueryableType(queryableType);
            }

            private class TypeReplacer : ExpressionVisitor
            {
                private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterMap;

                public TypeReplacer(IEnumerable<ParameterExpression> oldParams, IEnumerable<ParameterExpression> newParams)
                {
                    _parameterMap = oldParams.Zip(newParams, (oldParam, newParam) => new { oldParam, newParam })
                                             .ToDictionary(pair => pair.oldParam, pair => pair.newParam);
                }

                protected override Expression VisitParameter(ParameterExpression node)
                {
                    return _parameterMap.TryGetValue(node, out var replacement) ? replacement : node;
                }
            }
        }

    }
}
