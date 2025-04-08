using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class FullDataSourceSelectionTests : TestBase
    {

        [TestMethod]
        public void LambdaParameter_as_data_source_selected_in_projection_should_select_all_columns()
        {
            Expression<Func<object>> queryExpression = () =>
            queryProvider.DataSet<Student>()
            .Where(x => x.StudentId == "123")
            .Select(f => f);
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.StudentId = '123')";
            Test("Data Source Parameter Selection Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void LambdaParameter_as_data_source_selected_as_an_object_in_anonymous_type_in_projection()
        {
            Expression<Func<object>> queryExpression = () =>
            queryProvider.DataSet<Student>()
            .Where(x => x.StudentId == "123")
            .Select(x => new { Std = x, STD_ID = x.StudentId });
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_1.StudentId as STD_ID
	from	Student as a_1
	where	(a_1.StudentId = '123')";
            Test("Data Source Parameter Selection Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void Multiple_data_sources_added_using_From_query_method_then_one_data_source_selected_in_projection_within_anonymous_type()
        {
            var q =
            queryProvider.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>()
            })
            .Where(x => x.s.StudentId == "123")
            .Select(x => new { x.s, x.sg.Grade });
            string? expectedResult = @$"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.Grade as Grade
	from	Student as a_1
		cross join StudentGrade as a_2
	where	(a_1.StudentId = '123')
";
            Test("Data Source Selected As Whole In Multi Data Source Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Multiple_data_sources_added_using_From_query_method_then_one_data_source_selected_in_projection()
        {
            var q =
            queryProvider.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>()
            })
            .Where(x => x.s.StudentId == "123")
            .Select(x => x.s);
            string? expectedResult = @$"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
		cross join StudentGrade as a_2
	where	(a_1.StudentId = '123')
";
            Test("Data Source Selected As Whole In Multi Data Source Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Multiple_data_sources_added_using_From_query_method_then_LambdaParameter_selected_in_projection()
        {
            var q =
            queryProvider.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>()
            })
            .Where(x => x.s.StudentId == "123")
            .Select(f => f);
            string? expectedResult = @$"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.RowId as RowId, a_2.StudentId as StudentId_1, a_2.Grade as Grade
	from	Student as a_1
		cross join StudentGrade as a_2
	where	(a_1.StudentId = '123')
";
            Test("Parameter Selection In Multi Data Source Query Test", q.Expression, expectedResult);
        }

    }
}
