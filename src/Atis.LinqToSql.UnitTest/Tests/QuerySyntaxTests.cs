using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest.Tests
{
    [TestClass]
    public class QuerySyntaxTests : TestBase
    {

        [TestMethod]
        public void Query_syntax_with_multiple_children_navigation()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    from ed in e.NavDegrees
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
";
            Test("Query Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_standard_join()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    join ed in employeeDegrees on new { e.RowId, e.EmployeeId } equals new { ed.RowId, ed.EmployeeId }
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as a_2 on ((a_1.RowId = a_2.RowId) and (a_1.EmployeeId = a_2.EmployeeId))
";
            Test("Standard Join Multiple Fields Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Query_syntax_standard_join_using_DefaultIfEmpty_should_translate_to_outer_join()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    join ed in employeeDegrees on new { e.RowId, e.EmployeeId } equals new { ed.RowId, ed.EmployeeId }
                    into eds
                    from ed2 in eds.DefaultIfEmpty()
                    join m in employees on e.ManagerId equals m.EmployeeId
                    into ms
                    from m2 in ms.DefaultIfEmpty()
                    select new { e.EmployeeId, e.Name, ed2.Degree, ed2.University, ManagerName = m2.Name }
                    ;

            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University, a_3.Name as ManagerName
	from	Employee as a_1
		left join EmployeeDegree as a_2 on ((a_1.RowId = a_2.RowId) and (a_1.EmployeeId = a_2.EmployeeId))
		left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
";
            Test("Standard Join Left Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Query_syntax_having_standard_join_with_projection()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    join ed in employeeDegrees on e.EmployeeId equals ed.EmployeeId
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
";
            Test("Standard Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_three_data_sources_selected_should_translate_to_cross_join_for_all_data_sources()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            IQueryable<Employee> managers = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    from ed in employeeDegrees
                    from m in managers
                    from m2 in managers
                    from m3 in managers
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University, ManagerName = m.Name, Manager2Name = m2.Name, Manager3Name = m3.Name }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University, a_3.Name as ManagerName, a_4.Name as Manager2Name, a_5.Name as Manager3Name
	from	Employee as a_1
		cross join EmployeeDegree as a_2
		cross join Employee as a_3
		cross join Employee as a_4
        cross join Employee as a_5
";
            Test("3 Data Sources Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_multiple_data_sources_added_using_children_navigation()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    from ed in e.NavDegrees
                    from ms in ed.NavMarksheets
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University, ms.Course, ms.Grade }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University, NavMarksheets_3.Course as Course, NavMarksheets_3.Grade as Grade
	from	Employee as a_1
		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
		inner join Marksheet as NavMarksheets_3 on (NavDegrees_2.RowId = NavMarksheets_3.EmployeeDegreeRowId)
";
            Test("Query Syntax Multiple From Navigation Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Query_syntax_with_sub_query_having_Where_should_translate_to_inner_join()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId)
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, child_join_2.Degree as Degree, child_join_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as child_join_2 on (child_join_2.EmployeeId = a_1.EmployeeId)
";
            Test("Query Syntax From with Where Converted to Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_sub_query_having_Where_with_outer_query_LambdaParameter_used_and_Take_should_translate_to_cross_apply()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Take(5)
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		cross apply (
			select	top (5)	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
			from	EmployeeDegree as a_2
			where	(a_2.EmployeeId = a_1.EmployeeId)
		) as a_3
";
            Test("Query Syntax From with Where But 'Take' at End Convert to Cross Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_multiple_children_navigation_having_DefaultIfEmpty_applied_should_translate_to_outer_join()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    from ed in e.NavDegrees.DefaultIfEmpty()
                    from ms in ed.NavMarksheets
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University, ms.Course, ms.Grade }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University, NavMarksheets_3.Course as Course, NavMarksheets_3.Grade as Grade
	from	Employee as a_1
		left join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
		inner join Marksheet as NavMarksheets_3 on (NavDegrees_2.RowId = NavMarksheets_3.EmployeeDegreeRowId)
";
            Test("Query Syntax Multi From Navigation one as Default If Empty Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_sub_query_having_Where_applied_with_no_outer_query_LambdaParameter_used_should_translate_to_cross_join()
        {
            var employees = dbc.DataSet<Employee>();
            var employeeDegrees = dbc.DataSet<EmployeeDegree>();
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.Degree == "123")
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		cross join (
			select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
			from	EmployeeDegree as a_2
			where	(a_2.Degree = '123')
		) as a_3
";
            Test("Query Syntax Multi From Sub Query to Cross Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_sub_query_having_Where_and_Select_applied_but_outer_query_LambdaParameter_used_in_Select_should_translate_to_cross_apply()
        {
            var employees = dbc.DataSet<Employee>();
            var employeeDegrees = dbc.DataSet<EmployeeDegree>();
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.Degree == "123").Select(x => new { x.Degree, x.University, e.Department })
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		cross apply (
			select	a_2.Degree as Degree, a_2.University as University, a_1.Department as Department
			from	EmployeeDegree as a_2
			where	(a_2.Degree = '123')
		) as a_3
";
            Test("Query Syntax Multi From Sub Query to Cross Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_sub_query_having_Where_and_Select_applied_but_outer_query_LambdaParameter_used_in_Select_and_DefaultIfEmpty_applied_should_translate_to_outer_apply()
        {
            var employees = dbc.DataSet<Employee>();
            var employeeDegrees = dbc.DataSet<EmployeeDegree>();
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.Degree == "123").Select(x => new { x.Degree, x.University, e.Department }).DefaultIfEmpty()
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		outer apply (
			select	a_2.Degree as Degree, a_2.University as University, a_1.Department as Department
			from	EmployeeDegree as a_2
			where	(a_2.Degree = '123')
		) as a_3
";
            Test("Query Syntax Multi From Sub Query to Cross Apply Default If Empty Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_with_multiple_projections_should_translate_into_sub_queries()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    select new { e.EmployeeId, e.Name }
                    into r1
                    select new { Col1 = r1.EmployeeId, Col2 = r1.Name }
                    into r2
                    select new { r2.Col1, r2.Col2 }
                    ;
            string? expectedResult = @"
select	a_3.Col1 as Col1, a_3.Col2 as Col2
	from	(
		select	a_2.EmployeeId as Col1, a_2.Name as Col2
		from	(
			select	a_1.EmployeeId as EmployeeId, a_1.Name as Name
			from	Employee as a_1
		) as a_2
	) as a_3
";
            Test("Query Syntax Select Multiple Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Query_syntax_multiple_children_navigation_added_with_multiple_projection_should_translate_to_inner_join_and_wrapped_sub_queries()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q =
                    from e in employees
                    from ed in e.NavDegrees
                    from subOrdinate in e.NavSubOrdinates
                    from subOrdinateDegree in subOrdinate.NavDegrees
                    select new { e.EmployeeId, e.Name, ed.Degree, SubOrdinate = new { n1 = new { n1 = subOrdinate }, f2 = subOrdinate.EmployeeId }, SubOrdinateDegree = subOrdinateDegree.Degree }
                    into final
                    select new { z = final.SubOrdinate.n1 }
                    into next
                    select new { y = next.z.n1.ManagerId }
                    ;
            string? expectedResult = @"
select	a_6.ManagerId as y
	from	(
		select	a_5.RowId as RowId, a_5.EmployeeId_1 as EmployeeId_1, a_5.Name_1 as Name_1, a_5.Department as Department, a_5.ManagerId as ManagerId
		from	(
			select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavSubOrdinates_3.RowId as RowId, NavSubOrdinates_3.EmployeeId as EmployeeId_1, NavSubOrdinates_3.Name as Name_1, NavSubOrdinates_3.Department as Department, NavSubOrdinates_3.ManagerId as ManagerId, NavSubOrdinates_3.EmployeeId as f2, NavDegrees_4.Degree as SubOrdinateDegree
			from	Employee as a_1
				inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
				inner join Employee as NavSubOrdinates_3 on (a_1.EmployeeId = NavSubOrdinates_3.ManagerId)
				inner join EmployeeDegree as NavDegrees_4 on (NavSubOrdinates_3.EmployeeId = NavDegrees_4.EmployeeId)
		) as a_5
	) as a_6
";
            Test("Query Syntax with Multiple Data Sources having Navigation Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Query_syntax_multiple_data_sources_selected_in_projection()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);

            var q = from asset in assets
                    join item in items on asset.ItemId equals item.ItemId
                    where asset.SerialNumber == "123"
                    select new { asset, item };

            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber, a_2.ItemId as ItemId_1, a_2.ItemDescription as ItemDescription
	from	Asset as a_1
		inner join ItemBase as a_2 on (a_1.ItemId = a_2.ItemId)
	where	(a_1.SerialNumber = '123')
";
            Test($"Multiple Data Source Selected With Normal Join Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void GroupJoin_sub_query_should_translate_as_sub_query_on_every_instance()
        {
            var orders = new Queryable<Order>(this.dbc);
            var orderDetails = new Queryable<OrderDetail>(this.dbc);
            var customers = new Queryable<Customer>(this.dbc);

            // GroupJoin => (IQueryable<T>, IQueryable<O>, T.Key, O.Key, (T, IQueryable<O>, R) => new { p1 = T, p2 = IQueryable<O> })
            var q = from o in orders
                    join od in orderDetails on o.OrderID equals od.OrderID
                    join c in customers on o.CustomerId equals c.CustomerId into g
                    where g.Count() > 0
                    select new { o.OrderID, o.OrderDate, od.Quantity, od.UnitPrice, Count = g.Where(y => y.CustomerName.StartsWith("TT")).Count() };

            string? expectedResult = @"
select	a_1.OrderID as OrderID, a_1.OrderDate as OrderDate, a_2.Quantity as Quantity, a_2.UnitPrice as UnitPrice, (
		select	Count(1) as Col1
		from	Customer as a_3
		where	(a_1.CustomerId = a_3.CustomerId) and (a_3.CustomerName like ('TT' + '%'))
	) as Count
	from	Order as a_1
		inner join OrderDetail as a_2 on (a_1.OrderID = a_2.OrderID)
	where	((
		select	Count(1) as Col1
		from	Customer as a_3
		where	(a_1.CustomerId = a_3.CustomerId)
	) > 0)
";
            Test("GroupJoin with sub-query used in different places", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupJoin_sub_query_selected_in_projection()
        {
            var orders = new Queryable<Order>(this.dbc);
            var orderDetails = new Queryable<OrderDetail>(this.dbc);
            var customers = new Queryable<Customer>(this.dbc);

            // GroupJoin => (IQueryable<T>, IQueryable<O>, T.Key, O.Key, (T, IQueryable<O>, R) => new { p1 = T, p2 = IQueryable<O> })
            var q = from o in orders
                    join od in orderDetails on o.OrderID equals od.OrderID
                    join c in customers on o.CustomerId equals c.CustomerId into g          // g could be 4th parameter, but below select is modifying the
                    select new { o, od, g = g.Where(z => z.CustomerId == "123") } into t    // 4th parameter of GroupJoin
                                                                                            // if we don't use select new after 2nd join, the new anonymous type would be auto
                                                                                            // and below we didn't had to use the "t" variable, and would have used "g" directly
                                                                                            // but since we defined the anonymous type manually, there now we have to use the "t" variable
                    where t.g.Where(r => r.CustomerName.Contains("abc")).Count() > 0
                    select new { t.o.OrderID, t.o.OrderDate, t.od.Quantity, t.od.UnitPrice, Count = t.g.Where(y => y.CustomerName.StartsWith("TT")).Count() };

            string? expectedResult = @"
select	a_5.OrderID as OrderID, a_5.OrderDate as OrderDate, a_5.Quantity as Quantity, a_5.UnitPrice as UnitPrice, (
		select	Count(1) as Col1
		from	Customer as a_3
		where	(a_5.CustomerId = a_3.CustomerId) and (a_3.CustomerId = '123') and (a_3.CustomerName like ('TT' + '%'))
	) as Count
	from	(
		select	a_1.OrderID as OrderID, a_1.CustomerId as CustomerId, a_1.OrderDate as OrderDate, a_2.OrderID as OrderID_1, 
                a_2.Quantity as Quantity, a_2.UnitPrice as UnitPrice, 
                a_4.RowId as RowId, a_4.CustomerId as CustomerId_1, a_4.CustomerName as CustomerName, a_4.Address as Address, a_4.Status as Status
		from	Order as a_1
			inner join OrderDetail as a_2 on (a_1.OrderID = a_2.OrderID)
			outer apply (
				select	a_3.RowId as RowId, a_3.CustomerId as CustomerId, a_3.CustomerName as CustomerName, a_3.Address as Address, a_3.Status as Status
				from	Customer as a_3
				where	(a_1.CustomerId = a_3.CustomerId) and (a_3.CustomerId = '123')
			) as a_4
	) as a_5
	where	((
		select	Count(1) as Col1
		from	Customer as a_3
		where	(a_5.CustomerId = a_3.CustomerId) and (a_3.CustomerId = '123') and (a_3.CustomerName like ('%' + ('abc' + '%')))
	) > 0)
";
            Test("GroupJoin sub-query selected in projection", q.Expression, expectedResult);
        }
    }
}
