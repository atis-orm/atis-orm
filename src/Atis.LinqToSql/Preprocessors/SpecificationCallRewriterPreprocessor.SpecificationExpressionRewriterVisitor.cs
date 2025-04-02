using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{

public partial class SpecificationCallRewriterPreprocessor
    {
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
