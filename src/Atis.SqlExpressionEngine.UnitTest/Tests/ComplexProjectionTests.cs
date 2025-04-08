namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class ComplexProjectionTests : TestBase
    {

        [TestMethod]
        public void Multiple_data_sources_with_full_data_source_selected_in_nested_anonymous_type_along_with_normal_columns()
        {
            var queryProvider = new QueryProvider();
            var q = queryProvider                            .From(() => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                            .LeftJoin(x => x.ed, x => x.e.EmployeeId == x.ed.EmployeeId)
                            .LeftJoin(queryProvider.DataSet<Employee>(), (o, j) => new { o, m = j }, n => n.o.e.ManagerId == n.m.EmployeeId)
                            .Select(x => new { FullDetail = new { Employee = x.o.e, x.o.e.Name }, x.o.ed.RowId, x.m })
                            .Select(x => new { x.FullDetail.Employee.RowId, x.FullDetail.Employee.Name, EmployeeDegreeRowId = x.RowId, ManagerName = x.m.Name })

                    ;
            string? expectedResult = @"
select	a_4.RowId as RowId, a_4.Name as Name, a_4.RowId_1 as EmployeeDegreeRowId, a_4.Name_2 as ManagerName
	from	(
		select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_1.Name as Name_1, a_2.RowId as RowId_1, a_3.RowId as RowId_2, a_3.EmployeeId as EmployeeId_1, a_3.Name as Name_2, a_3.Department as Department_1, a_3.ManagerId as ManagerId_1
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
			left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
	) as a_4
";
            Test("Select Multi Level With Sub Query Test", q.Expression, expectedResult);
        }

        private class MultiLevelResult
        {
            public Employee Employee { get; set; }
            public EmployeeDegree EmployeeDegree { get; set; }
        }

        [TestMethod]
        public void Multiple_data_sources_with_full_data_source_selected_using_DTO_type_then_again_selected_fields_from_DTO()
        {
            var queryProvider = new QueryProvider();
            var q = queryProvider                            .From(() => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                            .LeftJoin(x => x.ed, x => x.e.EmployeeId == x.ed.EmployeeId)
                            .LeftJoin(queryProvider.DataSet<Employee>(), (o, j) => new { o, m = j }, n => n.o.e.ManagerId == n.m.EmployeeId)
                            .Select(x => new MultiLevelResult { Employee = x.o.e, EmployeeDegree = x.o.ed })
                            .Select(x => new { EmployeeRowId = x.Employee.RowId, DegreeRowId = x.EmployeeDegree.RowId, x.Employee.Name })
                            ;
            string? expectedResult = @"
select	a_4.RowId as EmployeeRowId, a_4.RowId_1 as DegreeRowId, a_4.Name as Name
	from	(
		select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
			left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
	) as a_4
";
            Test("Select Multi Level With Member Init Expression Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Fields_from_multiple_data_sources_selected_then_again_selected_few_field_on_top_of_that()
        {
            var provider = new QueryProvider();
            var employees = provider.DataSet<Employee>();
            var employeeDegrees = provider.DataSet<EmployeeDegree>();
            var q = employees
                .LeftJoin(employeeDegrees, (e, ed) => new { e, ed }, j => j.e.EmployeeId == j.ed.EmployeeId)
                .Select(x => new { x.e.EmployeeId, x.e.Name, x.ed.Degree })
                .Select(x => new { x.EmployeeId, x.Name })
                ;
            string? expectedResult = @"
select	a_3.EmployeeId as EmployeeId, a_3.Name as Name
	from	(
		select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
	) as a_3
";
            Test("Nested Select With Member Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Multiple_data_sources_selected_in_1_property_and_then_that_1_property_selected_in_projection_in_anonymous_type_should_select_all_the_columns_of_all_the_data_sources_under_that_1_property()
        {
            var q = queryProvider.DataSet<Employee>()
                    .LeftJoin(queryProvider.DataSet<EmployeeDegree>(), (e, ed) => new { e, ed }, j => j.e.EmployeeId == j.ed.EmployeeId)
                    .LeftJoin(queryProvider.DataSet<Employee>(), (o, m) => new { o, m }, n => n.o.e.ManagerId == n.m.EmployeeId)
                    .Select(x => new { t = x.o })
                    ;
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
		left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
";
            Test("Multi Data Source Selection Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Multiple_data_sources_selected_in_1_property_and_then_that_1_property_selected_in_projection_should_select_all_the_columns_of_all_the_data_sources_under_that_1_property()
        {
            var q = queryProvider.DataSet<Employee>()
                    .LeftJoin(queryProvider.DataSet<EmployeeDegree>(), (e, ed) => new { e, ed }, j => j.e.EmployeeId == j.ed.EmployeeId)
                    .LeftJoin(queryProvider.DataSet<Employee>(), (o, m) => new { o, m }, n => n.o.e.ManagerId == n.m.EmployeeId)
                    .Select(x => x.o)
                    ;
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
		left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
";
            Test("Multi Data Source Selection Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Full_query_selected_using_LambdaParameter_in_Select_then_navigation_used_later_in_another_Select_should_translate_to_sub_query_and_then_inner_join_on_sub_query()
        {
            var assets = new Queryable<Asset>(new QueryProvider());
            var q = assets.Where(x => x.NavItem().ItemId == "333" || x.NavItem().ItemId == "111").Select(x => x).Select(x => new { x.NavItem().ItemId, x.NavItem().ItemDescription });
            string? expectedResult = @"
select	NavItem_4.ItemId as ItemId, NavItem_4.ItemDescription as ItemDescription
	from	(
		select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
		from	Asset as a_1
			inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
		where	((NavItem_2.ItemId = '333') or (NavItem_2.ItemId = '111'))
	) as a_3
		inner join ItemBase as NavItem_4 on (NavItem_4.ItemId = a_3.ItemId)
";
            Test("Navigation Full Selection Test", q.Expression, expectedResult);
        }
    }
}
