using Atis.SqlExpressionEngine.ExpressionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class SelectManyChildJoinReplacementTests : TestBase
    {

        [TestMethod]
        public void Sub_query_in_SelectMany_to_ChildJoin_replacement()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => (x.Degree == "123" || x.Degree == "665") && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            //var selectManyJoinConverter = new ChildJoinReplacementPreprocessor();
            //var updatedExpression = selectManyJoinConverter.Visit(preprocessedExpression);
            Console.WriteLine(updatedExpression);
            if (updatedExpression is MethodCallExpression methodCallExpr &&
                methodCallExpr.Arguments.Skip(1).First() is UnaryExpression unaryExpression &&
                unaryExpression.Operand is LambdaExpression lambdaExpression &&
                lambdaExpression.Body is ChildJoinExpression childJoinCall &&
                childJoinCall.Query is MethodCallExpression childJoinArg1 &&
                childJoinArg1.Method.Name == "Where" &&
                childJoinArg1.Arguments[1] is UnaryExpression childJoinArg1Unary &&
                childJoinArg1Unary.Operand is LambdaExpression childJoinArg1Lambda &&
                childJoinArg1Lambda.Body is BinaryExpression childJoinArg1Binary &&
                childJoinArg1Binary.NodeType == ExpressionType.OrElse
                )
            {
                Console.WriteLine("Success");
            }
            else
            {
                Assert.Fail("Expression was not converted as expected");
            }
        }

        [TestMethod]
        public void Sub_query_having_or_else_in_SelectMany_should_skip_ChildJoin_replacement()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" || x.University == "55" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if (((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body as MethodCallExpression)?.Method.Name != "Where")
                Assert.Fail("Expression was updated");
            else
                Console.WriteLine("Success");
        }

        [TestMethod]
        public void Outer_LambdaParameter_is_used_in_select_part_of_sub_query_in_SelectMany_should_skip_ChildJoin_replacement()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Select(x => new { x.EmployeeId, x.Degree, e.Name, x.RowId }).Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if (((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body as MethodCallExpression)?.Method.Name != "Where")
                Assert.Fail("Expression was updated");
            else
                Console.WriteLine("Success");
        }

        [TestMethod]
        public void Select_used_in_sub_query_in_SelectMany_but_outer_LambdaParameter_was_not_used_should_replace_it_with_ChildJoin()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Select(x => new { x.EmployeeId, x.Degree, x.RowId }).Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if ((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body is ChildJoinExpression)
                Console.WriteLine("Success");
            else
                Assert.Fail("Expression was not updated");
        }

        [TestMethod]
        public void Sub_query_in_SelectMany_using_query_syntax_should_replace_with_ChildJoin()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId)
                    from m in employees.Where(x => x.EmployeeId == e.ManagerId)
                    select new { e.Name, ed.Degree, ManagerName = m.Name };
            Console.WriteLine(q.Expression);
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if ((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body is ChildJoinExpression)
                Console.WriteLine("Success");
            else
                Assert.Fail("Expression was not updated");
        }
    }
}
