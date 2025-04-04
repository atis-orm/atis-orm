using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest.Tests
{
    [TestClass]
    public class FromExtensionMethodTests : TestBase
    {

        [TestMethod]
        public void From_multiple_data_sources()
        {
            var q = dbc.From(() => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() });
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		cross join EmployeeDegree as a_2
";
            Test("Multiple Data Sources Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void From_multiple_sub_queries()
        {
            var q = dbc.From(() => new
            {
                s = dbc.DataSet<Student>().Where(x => x.Address.Contains("KHI")).Schema(),
                sg = dbc.DataSet<StudentGrade>().Where(x => x.Grade == "5").Schema(),
            });
            var expectedResult = @$"
select	a_2.StudentId as StudentId, a_2.Name as Name, a_2.Address as Address, a_2.Age as Age, a_2.AdmissionDate as AdmissionDate, a_2.RecordCreateDate as RecordCreateDate, a_2.RecordUpdateDate as RecordUpdateDate, a_2.StudentType as StudentType, a_2.CountryID as CountryID, a_2.HasScholarship as HasScholarship, a_4.RowId as RowId, a_4.StudentId as StudentId_1, a_4.Grade as Grade
	from	(
		select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
		from	Student as a_1
		where	(a_1.Address like ('%' + ('KHI' + '%')))
	) as a_2
		cross join (
			select	a_3.RowId as RowId, a_3.StudentId as StudentId, a_3.Grade as Grade
			from	StudentGrade as a_3
			where	(a_3.Grade = '5')
		) as a_4";
            Test("Multiple Sub Query Data Sources Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void From_mixed_data_sources_with_different_query_methods()
        {
            var q =
            dbc.From(() => new
            {
                s = dbc.DataSet<Student>().Where(x => x.Address.Contains("KHI")).Schema(),
                sg = QueryExtensions.Table<StudentGrade>(),
            })
            .Where(p1 => p1.s.StudentId == p1.sg.StudentId)
            .Take(5)
            .OrderBy(x => x.s.StudentId)
            .Select(x => new { x.s.StudentId, x.sg.Grade, SgStudentId = x.sg.StudentId });
            ;
            var expectedResult = @$"
select	a_4.StudentId as StudentId, a_4.Grade as Grade, a_4.StudentId_1 as SgStudentId
	from	(
		select	top (5)	a_2.StudentId as StudentId, a_2.Name as Name, a_2.Address as Address, a_2.Age as Age, a_2.AdmissionDate as AdmissionDate, a_2.RecordCreateDate as RecordCreateDate, a_2.RecordUpdateDate as RecordUpdateDate, a_2.StudentType as StudentType, a_2.CountryID as CountryID, a_2.HasScholarship as HasScholarship, a_3.RowId as RowId, a_3.StudentId as StudentId_1, a_3.Grade as Grade
		from	(
			select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
			from	Student as a_1
			where	(a_1.Address like ('%' + ('KHI' + '%')))
		) as a_2
			cross join StudentGrade as a_3
		where	(a_2.StudentId = a_3.StudentId)
	) as a_4
	order by a_4.StudentId asc
";
            Test("Multiple Sub Query Data Sources Complex Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void From_mixed_data_sources_then_new_data_source_added_using_dynamic_joining()
        {
            var queryProvider = new QueryProvider();
            var q = queryProvider                            .From(() => new
                            {
                                q1 = queryProvider.From(() => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                                                            .LeftJoin(f1 => f1.ed, fj1 => fj1.e.EmployeeId == fj1.ed.EmployeeId)
                                                            .Schema(),
                                m = QueryExtensions.Table<Employee>()
                            })
                            .LeftJoin(f2 => f2.m, fj2 => fj2.q1.e.ManagerId == fj2.m.EmployeeId)
                            .LeftJoin(new Queryable<Employee>(queryProvider), (o, j3) => new { o, m2 = j3 }, n => n.o.q1.e.ManagerId == n.m2.EmployeeId)
                            .Select(x => new { EmRowId = x.o.q1.e.RowId, EdRowId = x.o.q1.ed.RowId, M1RowId = x.o.m.RowId, M2RowId = x.m2.RowId })
                            ;
            string? expectedResult = @"
select	a_3.RowId as EmRowId, a_3.RowId_1 as EdRowId, a_4.RowId as M1RowId, a_5.RowId as M2RowId
	from	(
		select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
	) as a_3
		left join Employee as a_4 on (a_3.ManagerId = a_4.EmployeeId)
		left join Employee as a_5 on (a_3.ManagerId = a_5.EmployeeId)
";
            Test("Nested From Test", q.Expression, expectedResult);
        }
    }
}
