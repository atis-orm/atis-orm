using Atis.SqlExpressionEngine.ExpressionExtensions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class SelectManyChildJoinReplacementTests : TestBase
    {
        [TestMethod]
        public void Sub_query_having_or_else_in_SelectMany_should_skip_ChildJoin_replacement()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.queryProvider);
            var q = employees.SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" || x.University == "55" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            var methodName = (((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body as MethodCallExpression)?.Method.Name;
            Assert.IsTrue(methodName == "Where", "Expression was updated");
        }

        [TestMethod]
        public void Outer_LambdaParameter_is_used_in_select_part_of_sub_query_in_SelectMany_should_skip_ChildJoin_replacement()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.queryProvider);
            var q = employees.SelectMany(e => employeeDegrees.Select(x => new { x.EmployeeId, x.Degree, e.Name, x.RowId }).Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            var methodName2 = (((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body as MethodCallExpression)?.Method.Name;
            Assert.IsTrue(methodName2 == "Where", "Expression was updated");
        }
    }
}
