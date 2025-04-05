using Atis.LinqToSql.Preprocessors;
using System.Linq.Expressions;

namespace Atis.LinqToSql.UnitTest.Tests
{
    [TestClass]
    public class ExpressionPreprocessorTests : TestBase
    {
        [TestMethod]
        public void All_Should_Be_Rewritten_To_Not_Any_With_Inverted_Predicate()
        {
            // Arrange
            var employees = new Queryable<Employee>(this.dbc);
            var q = employees.Where(x => x.NavDegrees.All(y => y.University == "MIT"));

            // Act
            var preprocessor = new AllToAnyRewriterPreprocessor();
            var result = preprocessor.Preprocess(q.Expression);

            // Assert: expression must be a MethodCall to Where
            var whereCall = result as MethodCallExpression;
            Assert.IsNotNull(whereCall);
            Assert.AreEqual("Where", whereCall.Method.Name);

            // Extract the lambda from Where argument
            var wherePredicate = (whereCall.Arguments[1] as UnaryExpression)?.Operand as LambdaExpression;
            Assert.IsNotNull(wherePredicate);

            // Top-level body should be Not
            var notExpr = wherePredicate.Body as UnaryExpression;
            Assert.IsNotNull(notExpr);
            Assert.AreEqual(ExpressionType.Not, notExpr.NodeType);

            // Inside should be Any method call
            var anyCall = notExpr.Operand as MethodCallExpression;
            Assert.IsNotNull(anyCall);
            Assert.AreEqual("Any", anyCall.Method.Name);

            // Second argument to Any should be a lambda
            var anyPredicate = anyCall.Arguments[1] as LambdaExpression;
            Assert.IsNotNull(anyPredicate);

            // And the body of that lambda should be a Not(...)
            var innerNot = anyPredicate.Body as UnaryExpression;
            Assert.IsNotNull(innerNot);
            Assert.AreEqual(ExpressionType.Not, innerNot.NodeType);

            // Optional: ensure the condition inside Not is the original predicate (University == "MIT")
            var innerCondition = innerNot.Operand as BinaryExpression;
            Assert.IsNotNull(innerCondition);
            Assert.AreEqual(ExpressionType.Equal, innerCondition.NodeType);
        }


    }
}
