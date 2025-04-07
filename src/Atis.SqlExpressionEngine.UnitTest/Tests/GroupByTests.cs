using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class GroupByTests : TestBase
    {
        [TestMethod]
        public void GroupBy_on_multiple_columns()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .GroupBy(x => new { G1 = x.Address, G2 = x.Age })
            .Select(x => new
            {
                x.Key.G1,
                x.Key.G2,
                MaxStudentId = x.Max(y => y.StudentId),
                TotalLines = x.Count(),
                CL = dbc.DataSet<StudentGrade>().Where(y => y.StudentId == x.Max(z => z.StudentId)).Select(y => y.Grade).FirstOrDefault()
            })
            ;
            string? expectedResult = @"
select	a_1.Address as G1, a_1.Age as G2, Max(a_1.StudentId) as MaxStudentId, Count(1) as TotalLines, (
		select	top (1)	a_2.Grade as Col1
		from	StudentGrade as a_2
		where	(a_2.StudentId = Max(a_1.StudentId))
	) as CL
	from	Student as a_1
	group by a_1.Address, a_1.Age
";
            Test("Group By Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void GroupBy_on_single_column()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .GroupBy(x => x.Address)
            .Select(x => new { Add = x.Key, TotalLines = x.Count(), MaxLine = x.Max(y => y.StudentId) });
            string? expectedResult = @"
select  a_1.Address as Add, count(1) as TotalLines, max(a_1.StudentId) as MaxLine
from    Student as a_1
group by a_1.Address
";
            Test("Group By Scalar Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void GroupBy_on_single_column_then_join_the_result_with_other_table()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<StudentGrade>()
            .GroupBy(x => x.StudentId)
            .Select(x => x.Key)
            .LeftJoin(dbc.DataSet<Student>(), (g, s) => new { g, s }, j => j.g == j.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as Col1, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (a_2.Col1 = a_3.StudentId)
";
            Test("Group Join On Scalar Select Test", queryExpression.Body, expectedResult);
        }


        [TestMethod]
        public void GroupBy_on_single_column_then_join__the_result_with_other_table_and_perform_a_function_on_GroupBy_result_in_join_condition()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<StudentGrade>()
            .GroupBy(x => x.StudentId)
            .Select(x => x.Key)
            .LeftJoin(dbc.DataSet<Student>(), (g, s) => new { g, s }, j => j.g.Substring(0, 5) == j.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as Col1, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (substring(a_2.Col1, (0 + 1), 5) = a_3.StudentId)
";
            Test("Group Join On Scalar Select Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void Use_GroupBy_sub_query_in_From_query_method_data_source()
        {
            var q =
            dbc.From(() => new
            {
                g = dbc.DataSet<StudentGrade>().GroupBy(x => x.StudentId).Select(x => x.Key).Schema(),
                s = QueryExtensions.Table<Student>(),
            })
            .LeftJoin(x => x.s, x => x.g == x.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as Col1, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (a_2.Col1 = a_3.StudentId)
";
            Test("Group Join Multiple Data Source Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Complex_GroupBy_with_Having_then_projection_with_multiple_aggregates()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.GroupBy(x => new { x.ManagerId, x.Department })
                                .Having(x => x.Count() > 1)
                                .Select(b => new { b.Key.ManagerId, b.Key.Department, TotalLines = b.Count(), MaxV = b.Max(y => y.EmployeeId) })
                                .Select(c => c.MaxV)
                                ;
            string? expectedResult = @"
select	a_2.MaxV as Col1
	from	(
		select	a_1.ManagerId as ManagerId, a_1.Department as Department, Count(1) as TotalLines, Max(a_1.EmployeeId) as MaxV
		from	Employee as a_1
		group by a_1.ManagerId, a_1.Department
		having	(Count(1) > 1)
	) as a_2
";
            Test("Group By Having Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Complex_GroupBy_with_Having_then_Select_OrderBy_Where_then_again_Select()
        {
            var d = new DateTime(2020, 1, 1);
            var students = new Queryable<Student>(new QueryProvider());
            var q = students
                        .Where(x => x.StudentId == "123")
                        .Where(x => x.RecordCreateDate > d)
                        .GroupBy(x => new { x.CountryID, x.StudentType })
                        .Having(x => x.Count() > 1)
                        .Select(x => new { x.Key.CountryID, SType = x.Key.StudentType, MaxAdmDate = x.Max(y => y.AdmissionDate) })
                        .OrderBy(x => x.CountryID)
                        .Where(x => x.SType == "345")
                        .Select(x => x.CountryID)
                        ;
            string? expectedResult = @"
select	a_2.CountryID as Col1
	from	(
		select	a_1.CountryID as CountryID, a_1.StudentType as SType, Max(a_1.AdmissionDate) as MaxAdmDate
		from	Student as a_1
		where	(a_1.StudentId = '123')
			and	(a_1.RecordCreateDate > '2020-01-01 00:00:00')
		group by a_1.CountryID, a_1.StudentType
		having	(Count(1) > 1)
		order by CountryID asc
	) as a_2
	where	(a_2.SType = '345')
";
            Test("Complex Group By Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Where_on_GroupBy_should_translate_to_Having()
        {
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .GroupBy(x => x.Name)
                        .Where(x => x.Max(y => y.Age) > 20)
                        .Select(x => new { Name = x.Key, TotalLines = x.Count() })
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, Count(1) as TotalLines
from	Student as a_1
where	(a_1.Address like ('%' + ('City' + '%')))
group by a_1.Name
having	(Max(a_1.Age) > 20)";
            Test("Having Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void SelectMany_GroupBy_on_single_column_without_Select_at_the_end()
        {
            var students = new Queryable<Employee>(dbc);
            var q = students
                .SelectMany(s => s.NavDegrees)
                .GroupBy(c => c.University);

            string? expectedResult = @"
    select	NavDegrees_2.University as Col1
	from	Employee as a_1
		    inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
	group by NavDegrees_2.University
";

            Test("SelectMany GroupBy on single column without Select at the end", q.Expression, expectedResult);
        }


        [TestMethod]
        public void SelectMany_GroupBy_on_multiple_columns_without_Select_at_the_end()
        {
            var students = new Queryable<Employee>(dbc);
            var q = students
                .SelectMany(s => s.NavDegrees)
                .GroupBy(c => new { g1 = c.University, g2 = c.Degree } );

            string? expectedResult = @"
    select	NavDegrees_2.University as g1, NavDegrees_2.Degree as g2
	from	Employee as a_1
		    inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
	group by NavDegrees_2.University, NavDegrees_2.Degree
";

            Test("SelectMany GroupBy on multiple columns without Select at the end", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_Aggregate_Then_Filter_Test()
        {
            var employees = new Queryable<Employee>(dbc);

            var q = employees
                .GroupBy(s => s.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .Where(x => x.Count > 2);

            string? expectedResult = @"
    select	a_2.Department as Department, a_2.Count as Count
	from	(
		select	a_1.Department as Department, Count(1) as Count
		from	Employee as a_1
        group by a_1.Department
	) as a_2
	where   (a_2.Count > 2)
";

            Test("GroupBy followed by aggregation and filtered on aggregate should wrap correctly", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Where_Select_GroupBy_Select_Test()
        {
            var employees = new Queryable<Employee>(dbc);

            var q = employees
                .Where(e => e.Department != null)
                .Select(e => new { e.Name, e.Department })
                .GroupBy(x => x.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() });

            string? expectedResult = @"
    select	a_2.Department as Department, Count(1) as Count
	from	(
		select	a_1.Name as Name, a_1.Department as Department
		from	Employee as a_1
		where	(a_1.Department is not null)
	) as a_2
	group by a_2.Department
";

            Test("Where before Select followed by GroupBy and aggregate Select should translate cleanly without wrapping", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_MultipleKeys_With_MultipleAggregates_Test()
        {
            var invoiceDetails = new Queryable<InvoiceDetail>(dbc);

            var q = invoiceDetails
                .GroupBy(i => new { i.InvoiceId, i.ItemId })
                .Select(g => new
                {
                    g.Key.InvoiceId,
                    g.Key.ItemId,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => x.UnitPrice * x.Quantity)
                });

            string? expectedResult = @"
    select	a_1.InvoiceId as InvoiceId, a_1.ItemId as ItemId,
            sum(a_1.Quantity) as TotalQty,
            sum((a_1.UnitPrice * a_1.Quantity)) as TotalAmount
	from	InvoiceDetail as a_1
	group by a_1.InvoiceId, a_1.ItemId
";

            Test("GroupBy on multiple keys with multiple aggregates should translate correctly", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Select_Nested_GroupBy_In_SubQuery_Test()
        {
            var employees = new Queryable<Employee>(dbc);

            // although we don't recommend IQueryable<> to be selected within projection,
            // however, engine translates these sub-queries as outer apply

            var q = employees
                .Select(e => new
                {
                    e.Name,
                    DegreeGroups = e.NavDegrees                                     // this sub-query will be translated to outer-apply
                        .GroupBy(d => d.University)
                        .Select(g => new { g.Key, Count = g.Count() })
                });

            string? expectedResult = @"
select	a_1.Name as Name, a_3.Key as Key, a_3.Count as Count
	from	Employee as a_1
		outer apply (
			select	a_2.University as Key, Count(1) as Count
			from	EmployeeDegree as a_2
			where	(a_1.EmployeeId = a_2.EmployeeId)
			group by a_2.University
		) as a_3
";

            Test("Select with nested GroupBy inside projection should produce correlated subquery", q.Expression, expectedResult);
        }

    }
}
