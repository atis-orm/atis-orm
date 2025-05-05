namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class RecursiveQueryTests : TestBase
    {

        [TestMethod]
        public void Recursive_query_having_anchor_used_later_in_other_query()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .InnerJoin(anchorSource,
                                                            (recursiveMember, anchorMember) => new { anchor = anchorMember, recursive = recursiveMember },
                                                            newShape => newShape.anchor.EmployeeId == newShape.recursive.ManagerId)
                                                .Select(newShape => new { newShape.recursive.EmployeeId, newShape.recursive.Name, newShape.recursive.ManagerId }))
                        ;
            string? expectedResult = @"
   	with cte_1 as
	(
		select a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId
		from Employee as a_3
				inner join cte_1 as a_4 on (a_4.EmployeeId = a_3.ManagerId)
	)
	select a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.ManagerId as ManagerId
	from cte_1 as a_5
";
            Test("Recursive Anchor After Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_having_anchor_used_in_the_start_and_other_source_joined_to_anchor()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => anchorSource
                                                .InnerJoin(employees,
                                                            (anchorMember, recursiveMember) => new { anchor = anchorMember, recursive = recursiveMember },
                                                            newShape => newShape.anchor.EmployeeId == newShape.recursive.ManagerId)
                                                .Select(newShape => new { newShape.recursive.EmployeeId, newShape.recursive.Name, newShape.recursive.ManagerId }))
                        ;
            string? expectedResult = @"
with cte_1 as
(
	select a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId
	from Employee as a_2
	where (a_2.ManagerId is null)
	union all
	select a_4.EmployeeId as EmployeeId, a_4.Name as Name, a_4.ManagerId as ManagerId
	from cte_1 as a_3
			inner join Employee as a_4 on (a_3.EmployeeId = a_4.ManagerId)
)
select a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.ManagerId as ManagerId
from cte_1 as a_5
";
            Test("Recursive Before Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_having_anchor_used_as_exists_in_union()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .Where(o1 => anchorSource.Any(y => y.EmployeeId == o1.ManagerId))
                                                .Select(o2 => new { o2.EmployeeId, o2.Name, o2.ManagerId }))
                        ;
            string? expectedResult = @"
   	with cte_1 as
	(
		select a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId
		from Employee as a_3
		where exists(
				select 1 as Col1
				from cte_1 as a_4
				where (a_4.EmployeeId = a_3.ManagerId)
			)
	)
	select a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.ManagerId as ManagerId
	from cte_1 as a_5
";
            Test("Recursive Anchor In Exists Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_with_another_recursive_query_used_as_sub_query_in_projection()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .Where(o1 => anchorSource.Any(y => y.EmployeeId == o1.ManagerId))
                                                .Select(o2 => new { o2.EmployeeId, o2.Name, o2.ManagerId }))
                        //.Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .InnerJoin(employeeDegrees, (emp, deg) => new { emp, deg }, j => j.emp.EmployeeId == j.deg.EmployeeId)
                        .Select(x => new
                        {
                            x.emp.EmployeeId,
                            x.emp.Name,
                            x.emp.ManagerId,
                            x.deg.Degree,
                            x.deg.University,
                            f = employees.Where(u1 => u1.ManagerId == null)
                                        .Select(u1 => new { u1.EmployeeId, u1.ManagerId })
                                        .RecursiveUnion(as1 => employees.InnerJoin(as1, (e1, as1) => new { anchor1 = as1, recursive1 = e1 }, j1 => j1.anchor1.EmployeeId == j1.recursive1.ManagerId)
                                                                        .Select(s1 => new { s1.recursive1.EmployeeId, s1.recursive1.ManagerId }))
                                        .Select(s2 => s2.ManagerId)
                                        .FirstOrDefault()
                        })
                        ;
            string? expectedResult = @"
   	with cte_1 as
	(
		select a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId
		from Employee as a_3
		where exists(
				select 1 as Col1
				from cte_1 as a_4
				where (a_4.EmployeeId = a_3.ManagerId)
			)
	), cte_5 as
	(
		select a_6.EmployeeId as EmployeeId, a_6.ManagerId as ManagerId
		from Employee as a_6
		where (a_6.ManagerId is null)
		union all
		select a_7.EmployeeId as EmployeeId, a_7.ManagerId as ManagerId
		from Employee as a_7
				inner join cte_5 as a_8 on (a_8.EmployeeId = a_7.ManagerId)
	)
	select a_9.EmployeeId as EmployeeId, a_9.Name as Name, a_9.ManagerId as ManagerId, a_10.Degree as Degree, a_10.University as University, (
			select top (1) a_11.ManagerId as Col1
			from cte_5 as a_11
		) as f
	from cte_1 as a_9
			inner join EmployeeDegree as a_10 on (a_9.EmployeeId = a_10.EmployeeId)
";
            Test("Recursive Union With Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_with_query_syntax()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = (
                    from manager in employees
                    where manager.ManagerId == null
                    select new { manager.EmployeeId, manager.Name, manager.ManagerId }
                    )
                    .RecursiveUnion(anchorSource => from subOrdinate in employees
                                                    join anchorMember in anchorSource on subOrdinate.ManagerId equals anchorMember.EmployeeId
                                                    select new { subOrdinate.EmployeeId, subOrdinate.Name, subOrdinate.ManagerId })
                    .Select(outer => new { outer.EmployeeId, outer.Name, outer.ManagerId });
            string? expectedResult = @"
   	with cte_1 as
	(
		select a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId
		from Employee as a_3
				inner join cte_1 as a_4 on (a_3.ManagerId = a_4.EmployeeId)
	)
	select a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.ManagerId as ManagerId
	from cte_1 as a_5
";
            Test("Recursive Union With Query Syntax Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_used_as_sub_query_using_query_syntax()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());


            var recursiveQuery = (
                    from manager in employees
                    where manager.ManagerId == null
                    select new { manager.EmployeeId, manager.Name, manager.ManagerId }
                    )
                    .RecursiveUnion(anchorSource => from subordinate in employees
                                                    join anchorMember in anchorSource on subordinate.ManagerId equals anchorMember.EmployeeId
                                                    select new { subordinate.EmployeeId, subordinate.Name, subordinate.ManagerId })
                    .Select(outer => new { outer.EmployeeId, outer.Name, outer.ManagerId });


            var q = from degree in employeeDegrees
                    where recursiveQuery.Any(x => x.EmployeeId == degree.EmployeeId)
                    select degree;


            string? expectedResult = @"
with cte_1 as
	(
		select a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId
		from Employee as a_3
				inner join cte_1 as a_4 on (a_3.ManagerId = a_4.EmployeeId)
	)
	select a_5.RowId as RowId, a_5.EmployeeId as EmployeeId, a_5.Degree as Degree, a_5.University as University
	from EmployeeDegree as a_5
	where exists(
			select 1 as Col1
			from (
					select a_6.EmployeeId as EmployeeId, a_6.Name as Name, a_6.ManagerId as ManagerId
					from cte_1 as a_6
				) as a_7
			where (a_7.EmployeeId = a_5.EmployeeId)
		)
";

            Test("Recursive Union With Query Syntax on Sub-Query Level Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_having_navigation_used_on_anchor_with_SelectMany()
        {
            var employees = new Queryable<Employee>(new QueryProvider());

            var q = employees
                        .Where(x => x.ManagerId == null)
                        .RecursiveUnion(anchor => anchor.SelectMany(anchorMember => anchorMember.NavSubOrdinates))
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId });

            string? expectedResult = @"
    with cte_1 as
	(
		select a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select NavSubOrdinates_4.RowId as RowId, NavSubOrdinates_4.EmployeeId as EmployeeId, NavSubOrdinates_4.Name as Name, NavSubOrdinates_4.Department as Department, NavSubOrdinates_4.ManagerId as ManagerId
		from cte_1 as a_3
				inner join Employee as NavSubOrdinates_4 on (a_3.EmployeeId = NavSubOrdinates_4.ManagerId)
	)
	select a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.ManagerId as ManagerId
	from cte_1 as a_5
";
            Test("Recursive Union With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_used_as_sub_query_with_outer_query_LambdaParameter_used_inside_with_navigation()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from employeeDegree in employeeDegrees
                    where employeeDegree.EmployeeId == "123"
                    select new
                    {
                        employeeDegree.EmployeeId,
                        employeeDegree.NavEmployee().Name,
                        FirstManagerStartsWithA = (
                                                        from subOrdinate in employees
                                                        where subOrdinate.EmployeeId == employeeDegree.NavEmployee().EmployeeId     // outer data source is being used within CTE part here
                                                        select subOrdinate
                                                        )
                                                        .RecursiveUnion(anchorSource => from manager in employees
                                                                                        join subOrdinate in anchorSource on manager.EmployeeId equals subOrdinate.ManagerId
                                                                                        select manager)
                                                        .Where(manager => manager.Name.StartsWith("A"))
                                                        .Select(manager => manager.ManagerId)
                                                        .FirstOrDefault(),

                    };
            string? expectedResult = @"
    with cte_1 as
	(
		select a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University, NavEmployee_3.RowId as RowId_1, NavEmployee_3.EmployeeId as EmployeeId_1, NavEmployee_3.Name as Name, NavEmployee_3.Department as Department, NavEmployee_3.ManagerId as ManagerId
		from EmployeeDegree as a_2
				inner join Employee as NavEmployee_3 on (NavEmployee_3.EmployeeId = a_2.EmployeeId)
		where (a_2.EmployeeId = '123')
	), cte_4 as
	(
		select a_5.RowId as RowId, a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.Department as Department, a_5.ManagerId as ManagerId
		from Employee as a_5
				cross join cte_1 as a_6
		where (a_5.EmployeeId = a_6.EmployeeId_1)
		union all
		select a_7.RowId as RowId, a_7.EmployeeId as EmployeeId, a_7.Name as Name, a_7.Department as Department, a_7.ManagerId as ManagerId
		from Employee as a_7
				inner join cte_4 as a_8 on (a_7.EmployeeId = a_8.ManagerId)
	)
	select a_6.EmployeeId as EmployeeId, a_6.Name as Name, (
			select top (1) a_9.ManagerId as Col1
			from cte_4 as a_9
			where (a_9.Name like 'A' + '%')
		) as FirstManagerStartsWithA
	from cte_1 as a_6
";
            Test("Recursive Union With Sub Query Outer Data Source In Anchor Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_used_as_sub_query_with_navigation_selected_on_anchor_using_SelectMany()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from manager in employees
                    where manager.EmployeeId == "123"       // picking specific manager
                    select new
                    {
                        ManagerId = manager.EmployeeId,
                        ManagerName = manager.Name,
                        NestedCount = employees        // anchor
                                            .Where(immediateChild => immediateChild.ManagerId == manager.EmployeeId)
                                            .RecursiveUnion(anchor => anchor.SelectMany(anchorMember => anchorMember.NavSubOrdinates))
                                            .Count()
                    };


            string? expectedResult = @"
    with cte_1 as
	(
		select a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.EmployeeId = '123')
	), cte_3 as
	(
		select a_4.RowId as RowId, a_4.EmployeeId as EmployeeId, a_4.Name as Name, a_4.Department as Department, a_4.ManagerId as ManagerId
		from Employee as a_4
				cross join cte_1 as a_5
		where (a_4.ManagerId = a_5.EmployeeId)
		union all
		select NavSubOrdinates_7.RowId as RowId, NavSubOrdinates_7.EmployeeId as EmployeeId, NavSubOrdinates_7.Name as Name, NavSubOrdinates_7.Department as Department, NavSubOrdinates_7.ManagerId as ManagerId
		from cte_3 as a_6
				inner join Employee as NavSubOrdinates_7 on (a_6.EmployeeId = NavSubOrdinates_7.ManagerId)
	)
	select a_5.EmployeeId as ManagerId, a_5.Name as ManagerName, (
			select Count(1) as Col1
			from cte_3 as a_8
		) as NestedCount
	from cte_1 as a_5
";
            Test("Recursive Union In Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Recursive_query_encapsulated_within_navigation_property()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from manager in employees
                    where manager.EmployeeId == "123"       // picking specific manager
                    select new
                    {
                        ManagerId = manager.EmployeeId,
                        ManagerName = manager.Name,
                        FilteredNested = manager.NavNestedChildren.Where(x => x.NavEmployee().Department == "IT").Count(),
                    };


            string? expectedResult = @"
    with cte_1 as
	(
		select a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.EmployeeId = '123')
	), cte_3 as
	(
		select a_4.RowId as RowId, a_4.EmployeeId as EmployeeId, a_4.Name as Name, a_4.Department as Department, a_4.ManagerId as ManagerId
		from Employee as a_4
				cross join cte_1 as a_5
		where (a_5.EmployeeId = a_4.ManagerId)
		union all
		select NavSubOrdinates_7.RowId as RowId, NavSubOrdinates_7.EmployeeId as EmployeeId, NavSubOrdinates_7.Name as Name, NavSubOrdinates_7.Department as Department, NavSubOrdinates_7.ManagerId as ManagerId
		from cte_3 as a_6
				inner join Employee as NavSubOrdinates_7 on (a_6.EmployeeId = NavSubOrdinates_7.ManagerId)
	)
	select a_5.EmployeeId as ManagerId, a_5.Name as ManagerName, (
			select Count(1) as Col1
			from (
					select a_8.EmployeeId as EmployeeId, a_8.ManagerId as ImmediateManagerId, a_5.EmployeeId as TopManagerId
					from cte_3 as a_8
				) as a_9
					inner join Employee as NavEmployee_10 on (NavEmployee_10.EmployeeId = a_9.EmployeeId)
			where (a_9.TopManagerId = a_9.TopManagerId)
				 and (NavEmployee_10.Department = 'IT')
		) as FilteredNested
	from cte_1 as a_5
";
            Test("Recursive Union With Sub Query Join Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Recursive_query_with_auto_projection()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .RecursiveUnion(
                            anchorSource => employees.InnerJoin(
                                                            anchorSource,
                                                            (recursiveMember, anchorMember) => new { anchor = anchorMember, recursive = recursiveMember },
                                                            newShape => newShape.anchor.EmployeeId == newShape.recursive.ManagerId
                                                        )
                                                        .Select(newShape => newShape.recursive))
                        ;
            string? expectedResult = @"
    with cte_1 as
	(
		select a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select a_3.RowId as RowId, a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.Department as Department, a_3.ManagerId as ManagerId
		from Employee as a_3
				inner join cte_1 as a_4 on (a_4.EmployeeId = a_3.ManagerId)
	)
	select a_5.RowId as RowId, a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.Department as Department, a_5.ManagerId as ManagerId
	from cte_1 as a_5
";
            Test("Recursive With No Selection Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Recursive_in_From_method()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = queryProvider.From(() => new
            {
                t1 = QueryExtensions.Table<EmployeeExtension>(),
                t2 = employees.Where(m1 => m1.ManagerId == null).RecursiveUnion(anchor => anchor.SelectMany(m2 => m2.NavSubOrdinates)).Schema(),
            });
            string? expectedResult = @"
    with cte_1 as
	(
		select a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId
		from Employee as a_2
		where (a_2.ManagerId is null)
		union all
		select NavSubOrdinates_4.RowId as RowId, NavSubOrdinates_4.EmployeeId as EmployeeId, NavSubOrdinates_4.Name as Name, NavSubOrdinates_4.Department as Department, NavSubOrdinates_4.ManagerId as ManagerId
		from cte_1 as a_3
				inner join Employee as NavSubOrdinates_4 on (a_3.EmployeeId = NavSubOrdinates_4.ManagerId)
	)
	select a_5.Designation as Designation, a_5.RowId as RowId, a_5.EmployeeId as EmployeeId, a_5.Name as Name, a_5.Department as Department, a_5.ManagerId as ManagerId, a_6.RowId as RowId_1, a_6.EmployeeId as EmployeeId_1, a_6.Name as Name_1, a_6.Department as Department_1, a_6.ManagerId as ManagerId_1
	from EmployeeExtension as a_5
			cross join cte_1 as a_6
";
            Test("Recursive In From Method Test", q.Expression, expectedResult);
        }
    }
}
