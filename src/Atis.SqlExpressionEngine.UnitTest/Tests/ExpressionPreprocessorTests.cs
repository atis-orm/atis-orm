using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.Services;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class ExpressionPreprocessorTests : TestBase
    {
        [TestMethod]
        public void CanEvaluate_Should_Return_True_For_InlineArray()
        {
            // Arrange
            Expression expr = Expression.NewArrayInit(
                typeof(string),
                Expression.Constant("HR"),
                Expression.Constant("Finance")
            );

            var reflectionService = new ReflectionService(new ExpressionEvaluator()); // Or get via DI/test setup

            // Act
            bool result = reflectionService.CanEvaluate(expr);

            // Assert
            Assert.IsTrue(result, "Inline array should be evaluatable.");
        }

        [TestMethod]
        public void All_Should_Be_Rewritten_To_Not_Any_With_Inverted_Predicate()
        {
            // Arrange
            var employees = new Queryable<Employee>(this.queryProvider);
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

        [TestMethod]
        public void DirectArrayVariable_Contains_Should_Be_Rewritten_To_InValuesExpression()
        {
            var values = new[] { "HR", "Finance" };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => values.Contains(x.Department));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void InlineArray_Contains_Should_Be_Rewritten_To_InValuesExpression()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => new[] { "HR", "Finance" }.Contains(x.Department));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void NestedObjectMember_Contains_Should_Be_Rewritten_To_InValuesExpression()
        {
            var obj = new { Departments = new[] { "HR", "Finance" } };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => obj.Departments.Contains(x.Department));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void InlineArray_Any_Y_Equals_X_Should_Be_Rewritten_To_InValuesExpression()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => new[] { "HR", "Finance" }.Any(y => y == x.Department));

            var result = this.PreprocessExpression(q.Expression);

            Console.WriteLine(ExpressionPrinter.PrintExpression(result));

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void NestedObjectMember_Any_Y_Equals_X_Should_Be_Rewritten_To_InValuesExpression()
        {
            var obj = new { Departments = new[] { "HR", "Finance" } };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => obj.Departments.Any(y => y == x.Department));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void DirectArrayVariable_Any_Y_Equals_X_Should_Be_Rewritten_To_InValuesExpression()
        {
            var departments = new[] { "HR", "Finance" };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => departments.Any(y => y == x.Department));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void NestedObjectMember_Any_X_Equals_Y_Should_Be_Rewritten_To_InValuesExpression()
        {
            var obj = new { Departments = new[] { "HR", "Finance" } };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => obj.Departments.Any(y => x.Department == y));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        [TestMethod]
        public void DirectArrayVariable_Any_X_Equals_Y_Should_Be_Rewritten_To_InValuesExpression()
        {
            var departments = new[] { "HR", "Finance" };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => departments.Any(y => x.Department == y));

            var result = this.PreprocessExpression(q.Expression);

            AssertHasInValuesExpression(result);
        }

        private IQueryable<StudentExtension> GetQueryable()
        {
            return new Queryable<StudentExtension>(this.queryProvider);
        }

        private BooleanInPredicatePreprocessor GetBooleanToEqualPreprocessor()
        {
            return new BooleanInPredicatePreprocessor(new ReflectionService(new ExpressionEvaluator()));
        }

        [TestMethod]
        public void Where_clause_rewrites_nullable_bool_to_equal_true()
        {
            var query = GetQueryable().Where(x => x.HasScholarship ?? false);
            var transformed = this.GetBooleanToEqualPreprocessor().Visit(query.Expression);

            var lambda = ExtractLambda(transformed);
            var body = lambda?.Body as BinaryExpression;

            Assert.IsNotNull(body);
            Assert.AreEqual(ExpressionType.Equal, body.NodeType);
            Assert.IsTrue(IsConstantTrue(body.Right));
        }

        [TestMethod]
        public void OrElse_expression_rewrites_left_side()
        {
            var query = GetQueryable().Where(x => (x.HasScholarship ?? false) || x.IsDeleted == false);
            var transformed = this.GetBooleanToEqualPreprocessor().Visit(query.Expression);

            var lambda = ExtractLambda(transformed);
            var body = lambda?.Body as BinaryExpression;

            Assert.IsNotNull(body);
            Assert.AreEqual(ExpressionType.OrElse, body.NodeType);

            var left = body.Left as BinaryExpression;
            Assert.IsNotNull(left);
            Assert.AreEqual(ExpressionType.Equal, left.NodeType);
            Assert.IsTrue(IsConstantTrue(left.Right));
        }

        [TestMethod]
        public void Select_projection_does_not_rewrite()
        {
            var query = GetQueryable().Select(x => new { Flag = x.IsDeleted });
            var transformed = this.GetBooleanToEqualPreprocessor().Visit(query.Expression);

            var lambda = ExtractLambda(transformed);
            var newExpr = lambda?.Body as NewExpression;

            Assert.IsNotNull(newExpr);
            var arg = newExpr.Arguments.First() as MemberExpression;
            Assert.IsNotNull(arg);
            Assert.AreEqual(nameof(StudentExtension.IsDeleted), arg.Member.Name);
        }

        [TestMethod]
        public void OrderBy_does_not_rewrite()
        {
            var query = GetQueryable().OrderBy(x => x.IsDeleted);
            var transformed = this.GetBooleanToEqualPreprocessor().Visit(query.Expression);

            var lambda = ExtractLambda(transformed);
            var body = lambda?.Body as MemberExpression;

            Assert.IsNotNull(body);
            Assert.AreEqual(nameof(StudentExtension.IsDeleted), body.Member.Name);
        }

        [TestMethod]
        public void GroupBy_does_not_rewrite()
        {
            var query = GetQueryable().GroupBy(x => x.IsDeleted);
            var transformed = this.GetBooleanToEqualPreprocessor().Visit(query.Expression);

            var lambda = ExtractLambda(transformed);
            var body = lambda?.Body as MemberExpression;

            Assert.IsNotNull(body);
            Assert.AreEqual(nameof(StudentExtension.IsDeleted), body.Member.Name);
        }

        [TestMethod]
        public void ConditionalExpression_test_is_rewritten()
        {
            Expression<Func<StudentExtension, string>> expr = x => x.IsDeleted ? "Valid" : "Invalid";
            var transformed = this.GetBooleanToEqualPreprocessor().Visit(expr);

            var cond = ((LambdaExpression)transformed).Body as ConditionalExpression;
            Assert.IsNotNull(cond);

            var test = cond.Test as BinaryExpression;
            Assert.IsNotNull(test);
            Assert.AreEqual(ExpressionType.Equal, test.NodeType);
            Assert.IsTrue(IsConstantTrue(test.Right));
        }

        private static LambdaExpression? ExtractLambda(Expression expression)
        {
            // Drill down to the lambda from method call
            var methodCall = expression as MethodCallExpression;
            if (methodCall == null) return null;

            var quote = methodCall.Arguments[1] as UnaryExpression;
            if (quote != null && quote.NodeType == ExpressionType.Quote)
                return quote.Operand as LambdaExpression;

            return null;
        }

        private static bool IsConstantTrue(Expression expr)
        {
            var constExpr = expr as ConstantExpression;
            return constExpr != null &&
                   constExpr.Type == typeof(bool) &&
                   (bool)constExpr.Value == true;
        }


        private static void AssertHasInValuesExpression(Expression expr)
        {
            var found = ExpressionTypeFinder.Find(expr, typeof(InValuesExpression));
            Assert.IsTrue(found, "Expected InValuesExpression but none found.");
        }

        private class ExpressionTypeFinder : ExpressionVisitor
        {
            private readonly Type typeToFind;
            public ExpressionTypeFinder(Type typeToFind)
            {
                this.typeToFind = typeToFind;
            }

            public bool ExpressionFound { get; private set; }

            public static bool Find(Expression expression, Type typeToFind)
            {
                if (expression == null)
                    throw new ArgumentNullException(nameof(expression));
                var finder = new ExpressionTypeFinder(typeToFind);
                finder.Visit(expression);
                return finder.ExpressionFound;
            }

            public override Expression Visit(Expression node)
            {
                if (this.ExpressionFound)
                    return node;

                if (node != null && node.GetType() == typeToFind)
                {
                    this.ExpressionFound = true;
                    return node;
                }

                return base.Visit(node);
            }
        }
    }
}
