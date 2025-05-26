using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class GroupByTests : TestBase
    {
        [TestMethod]
        public void GroupBy_on_multiple_columns()
        {
            Expression<Func<object>> queryExpression = () =>
            queryProvider.DataSet<Student>()
            .GroupBy(x => new { G1 = x.Address, G2 = x.Age })
            .Select(x => new
            {
                x.Key.G1,
                x.Key.G2,
                MaxStudentId = x.Max(y => y.StudentId),
                TotalLines = x.Count(),
                CL = queryProvider.DataSet<StudentGrade>().Where(y => y.StudentId == x.Max(z => z.StudentId)).Select(y => y.Grade).FirstOrDefault()
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
            queryProvider.DataSet<Student>()
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
            queryProvider.DataSet<StudentGrade>()
            .GroupBy(x => x.StudentId)
            .Select(x => x.Key)
            .LeftJoin(queryProvider.DataSet<Student>(), (g, s) => new { g, s }, j => j.g == j.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as g, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
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
            queryProvider.DataSet<StudentGrade>()
            .GroupBy(x => x.StudentId)
            .Select(x => x.Key)
            .LeftJoin(queryProvider.DataSet<Student>(), (g, s) => new { g, s }, j => j.g.Substring(0, 5) == j.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as g, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (substring(a_2.Col1, 0, 5) = a_3.StudentId)
";
            Test("Group Join On Scalar Select Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void Use_GroupBy_sub_query_in_From_query_method_data_source()
        {
            var q =
            queryProvider.From(() => new
            {
                g = queryProvider.DataSet<StudentGrade>().GroupBy(x => x.StudentId).Select(x => x.Key).Schema(),
                s = QueryExtensions.Table<Student>(),
            })
            .LeftJoin(x => x.s, x => x.g == x.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as g, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
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
where	(a_1.Address like '%' + 'City' + '%')
group by a_1.Name
having	(Max(a_1.Age) > 20)";
            Test("Having Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void SelectMany_GroupBy_on_single_column_without_Select_at_the_end()
        {
            var students = new Queryable<Employee>(queryProvider);
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
            var students = new Queryable<Employee>(queryProvider);
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
            var employees = new Queryable<Employee>(queryProvider);

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
            var employees = new Queryable<Employee>(queryProvider);

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
            var invoiceDetails = new Queryable<InvoiceDetail>(queryProvider);

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
            var employees = new Queryable<Employee>(queryProvider);

            // although we don't recommend IQueryable<> to be selected within projection,
            // however, engine translates these sub-queries as outer apply

            var q = employees
                .Select(e => new
                {
                    e.Name,
                    DegreeGroups = e.NavDegrees                                     // this sub-query will be translated to outer-apply
                        .GroupBy(d => d.University)
                        .Select(g => new { University = g.Key, TotalDegrees = g.Count() })
                        .FirstOrDefault()
                });

            string? expectedResult = @"
    select a_1.Name as Name, DegreeGroups_2.University as University, DegreeGroups_2.TotalDegrees as TotalDegrees
	from Employee as a_1
			outer apply (
				select top (1) a_3.University as University, Count(1) as TotalDegrees
				from EmployeeDegree as a_3
				where (a_1.EmployeeId = a_3.EmployeeId)
				group by a_3.University
			) as DegreeGroups_2
";

            Test("Select with nested GroupBy inside projection should produce correlated subquery", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_with_Having()
        {
            var studentAttendance = new Queryable<StudentAttendance>(queryProvider);
            var q = studentAttendance
                        .GroupBy(x => new { x.AttendanceDate.Value.Year, x.AttendanceDate.Value.Day })
                        .Having(a => a.Count() > 1)
                        .Select(b => new { b.Key.Year, b.Key.Day, TotalLines = b.Count(), MaxDate = b.Max(y => y.AttendanceDate) })
                        .Select(c => c.MaxDate)
                        ;
            string? expectedResult = @"
select	a_2.MaxDate as Col1
	from	(
		select	datepart(year, a_1.AttendanceDate) as Year, datepart(day, a_1.AttendanceDate) as Day, Count(1) as TotalLines, Max(a_1.AttendanceDate) as MaxDate
		from	StudentAttendance as a_1
		group by datepart(year, a_1.AttendanceDate), datepart(day, a_1.AttendanceDate)
		having	(Count(1) > 1)
	) as a_2
";
            Test("Group By Having Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_with_Having_then_Select_OrderBy_Where_Select()
        {
            var students = new Queryable<Student>(queryProvider);
            var d = new DateTime(2020, 1, 1);
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
		where	(a_1.StudentId = '123') and (a_1.RecordCreateDate > '2020-01-01 00:00:00')
		group by a_1.CountryID, a_1.StudentType
		having	(Count(1) > 1)
		order by CountryID asc
	) as a_2
	where	(a_2.SType = '345')
";
            Test("GroupBy with Having then Select OrderBy Where Select", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_on_year_month()
        {
            var studentAttendances = new Queryable<StudentAttendance>(queryProvider);
            var q = studentAttendances
                        .GroupBy(x => new { x.AttendanceDate.Value.Year, x.AttendanceDate.Value.Month })
                        .Select(x => new { x.Key.Year, x.Key.Month, TotalLines = x.Count() })
                        ;

            string? expectedResult = @"
select	datePart(Year, a_1.AttendanceDate) as Year, datePart(Month, a_1.AttendanceDate) as Month, Count(1) as TotalLines
	from	StudentAttendance as a_1
	group by datePart(Year, a_1.AttendanceDate), datePart(Month, a_1.AttendanceDate)
";

            Test("GroupBy on year month", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_on_fields_select_all_fields_then_again_group_by_on_1_field_then_having_and_select()
        {
            var studentAttendances = new Queryable<StudentAttendance>(queryProvider);
            var q = studentAttendances
                        .GroupBy(x => new { x.StudentId, x.AttendanceDate.Value.Year, x.AttendanceDate.Value.Month })
                        .Select(x => new { x.Key.StudentId, x.Key.Year, x.Key.Month })
                        .GroupBy(x => new { x.StudentId })
                        .Where(x => x.Min(y => y.Year) == 2002)
                        .Select(x => new { ID = x.Key.StudentId, TotalLines = x.Count() })
                        ;

            string? expectedResult = @"
select	a_2.StudentId as ID, Count(1) as TotalLines
	from	(
		select	a_1.StudentId as StudentId, datePart(Year, a_1.AttendanceDate) as Year, datePart(Month, a_1.AttendanceDate) as Month
		from	StudentAttendance as a_1
		group by a_1.StudentId, datePart(Year, a_1.AttendanceDate), datePart(Month, a_1.AttendanceDate)
	) as a_2
	group by a_2.StudentId
	having	(Min(a_2.Year) = 2002)
";

            Test("GroupBy on fields select all fields then again group by on 1 field then having and select", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Full_Key_anonymous_object_selection()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.GroupBy(x => new { x.StudentType, x.CountryID })
                        .Select(x => x.Key)
                        .OrderBy(x => x.CountryID);

            string? expectedResult = @"
select	a_1.StudentType as StudentType, a_1.CountryID as CountryID
	from	Student as a_1
	group by a_1.StudentType, a_1.CountryID
	order by CountryID asc
";
            Test("Full Key anonymous object selection", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_select_non_grouped_field_using_String_Join()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students
                        .GroupBy(x => x.CountryID)
                        .Select(g => new { CountryId = g.Key, StudentTypes = string.Join(", ", g.Select(y => y.StudentType)) });
            string? expectedResult = @"
select	a_1.CountryID as CountryId, JoinAggregate(a_1.StudentType, ', ') as StudentTypes
	from	Student as a_1
	group by a_1.CountryID
";
            Test("GroupBy select non grouped field", q.Expression, expectedResult);
        }

        [TestMethod]
        public void GroupBy_using_custom_String_Agg_function()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students
                        .GroupBy(x => x.CountryID)
                        .Select(g => new { CountryId = g.Key, StudentTypes = g.String_Agg(y => y.StudentType, ", ") });
            string? expectedResult = @"
select	a_1.CountryID as CountryId, string_agg(a_1.StudentType, ', ') as StudentTypes
	from	Student as a_1
	group by a_1.CountryID
";
            Test("GroupBy using custom String_Agg function", q.Expression, expectedResult);
        }


        [TestMethod]
        public void String_Concat_on_GroupBy()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students
                        .GroupBy(x => x.CountryID)
                        .Select(g => new { CountryId = g.Key, StudentTypes = string.Concat(g.Select(y => y.StudentType)) });
            string? expectedResult = @"
select	a_1.CountryID as CountryId, ConcatAggregate(a_1.StudentType) as StudentTypes
	from	Student as a_1
	group by a_1.CountryID
";
            Test("GroupBy Concat on GroupBy", q.Expression, expectedResult);
        }



        [TestMethod]
        public void Queryable_Selecting_Grouped_9()
        {
            var peopleList = new Queryable<Person>(queryProvider);
            var shoesList = new Queryable<Shoes>(queryProvider);
            var q = from Person person1
                  in from Person person2
                         in peopleList
                     select person2
                    join Shoes shoes
                        in shoesList
                        on person1.Age equals shoes.Age
                    group shoes by
                        new
                        {
                            person1.Id,
                            shoes.Style,
                            shoes.Age
                        }
                    into temp
                    select
                        new
                        {
                            temp.Key.Id,
                            temp.Key.Age,
                            temp.Key.Style,
                            Values = from t
                                            in temp
                                     select
                                             new
                                             {
                                                 t.Id,
                                                 t.Style,
                                                 t.Age,
                                             }
                        };

            string? expectedResult = null;
            Test("Queryable Selecting Grouped Row Test-9", q.Expression, expectedResult);
        }
    }
}
