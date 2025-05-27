namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class SelectManyTests : TestBase
    {

        [TestMethod]
        public void SelectMany_simple_should_translate_cross_join()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = employees
                    .SelectMany(e => employeeDegrees)
                    ;
            string expectedResult = $@"
select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		cross join EmployeeDegree as a_2
";
            Test("Query Select Many Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void SelectMany_with_sub_query_having_custom_projection_should_translate_to_cross_apply_on_sub_query()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = employees
                    .SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Select(x => new { x.EmployeeId, x.Degree }))
                    ;
            string expectedResult = $@"
select a_2.EmployeeId as EmployeeId, a_2.Degree as Degree
	from Employee as a_1
			inner join (
				select a_3.EmployeeId as EmployeeId, a_3.Degree as Degree
				from EmployeeDegree as a_3
			) as a_2 on (a_2.EmployeeId = a_1.EmployeeId)
";
            Test("Query Select Many With Where and Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void SelectMany_with_multiple_children_navigation_selected_should_translate_to_inner_join()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                    .SelectMany(e => e.NavDegrees)
                    ;
            string expectedResult = @"
select	NavDegrees_2.RowId as RowId, NavDegrees_2.EmployeeId as EmployeeId, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
    	from	Employee as a_1
    		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
";
            Test("Query Select Many With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void SelectMany_children_navigation_selected_then_Where_OrderBy_Select_should_translate_into_inner_join_with_later_operations_applied_on_child()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                    .SelectMany(e => e.NavDegrees)
                    .Where(x => x.Degree == "!23")
                    .OrderBy(x => x.University)
                    .Select(x => new { x.Degree, x.University })
                    ;
            string expectedResult = @"
select	NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
	where	(NavDegrees_2.Degree = '!23')
	order by NavDegrees_2.University asc
";
            Test("Query Select Many With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void SelectMany_children_navigation_selected_with_projection_parameter_used_should_translate_into_inner_join_with_custom_projection()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                    .SelectMany(e => e.NavDegrees, (e, ed) => new { e.EmployeeId, e.Name, ed.Degree, ed.University })
                    ;
            string expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
    	from	Employee as a_1
    		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
";
            Test("Query Select Many With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void SelectMany_with_sub_query_having_Where_and_custom_projection_parameter_used_should_translate_into_inner_join_with_custom_projection()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());

            var q = employees
                    .SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId), (e, ed) => new { e.EmployeeId, e.Name, ed.Degree, ed.University })
                    ;
            string expectedResult = @"
select a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University
	from Employee as a_1
			inner join EmployeeDegree as a_2 on (a_2.EmployeeId = a_1.EmployeeId)
";
            Test("Query Select Many With Select Test", q.Expression, expectedResult);
        }

    }
}
