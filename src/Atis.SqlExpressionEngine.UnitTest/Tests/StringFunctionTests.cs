using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class StringFunctionTests : TestBase
    {
        [TestMethod]
        public void String_Concat_method_test()
        {
            var q = queryProvider.Select(() => new { C1 = string.Concat(new object[] { "1", 2, "3" }), C2 = string.Concat("abc", "def") });
            string expectedResult = @"
select	a_1.C1 as C1, a_1.C2 as C2
from	(
    select	Concat('1', 2, '3') as C1, Concat('abc', 'def') as C2
) as a_1";

            Test("String concat method test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void String_Join_method_test()
        {
            var q = queryProvider.Select(() => new { C1 = string.Join(", ", new object[] { "1", 2, "3" }), C2 = string.Join(", ", "abc", "def") });
            string expectedResult = @"
select	a_1.C1 as C1, a_1.C2 as C2
from	(
    select	Join('1', 2, '3', ', ') as C1, Join('abc', 'def', ', ') as C2    		
) as a_1
";
            Test("String join method test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void ToString_method_should_translate_to_cast_test()
        {
            var q = queryProvider.Select(() => new { C1 = 123.ToString(), C2 = 123.456.ToString() });
            string expectedResult = @"
select	a_1.C1 as C1, a_1.C2 as C2
from	(
    select	cast(123 as NonUnicodeString(max)) as C1, cast(123.456 as NonUnicodeString(max)) as C2    		
) as a_1
";
            Test("ToString method test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void String_compare()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Where(x => string.Compare(x.Name, "A") > 0);
            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Name > 'A')
";

            Test("String compare", q.Expression, expectedResult);
        }

        [TestMethod]
        public void String_CompareTo()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Where(x => x.Name.CompareTo("A") >= 0);
            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Name >= 'A')
";
            Test("String CompareTo", q.Expression, expectedResult);
        }

        [TestMethod]
        public void String_different_method_calls()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Select(x => new
            {
                C1 = x.Name.Trim(),
                C2 = x.Name.TrimStart(),
                C3 = x.Name.TrimEnd(),
                C4 = x.Name.IndexOf("a"),
                C5 = x.Name.Length,
                C6 = x.Name.Replace("a", "b"),
                C7 = x.Name.ToUpper(),
                C8 = x.Name.ToLower(),
                C9 = x.Name.Substring(4),
                C10 = x.Name.Substring(6, 3),
            });
            string expectedResult = @"
select	Trim(a_1.Name) as C1, TrimStart(a_1.Name) as C2, TrimEnd(a_1.Name) as C3, CharIndex(a_1.Name, 'a') as C4, CharLength(a_1.Name) as C5, Replace(a_1.Name, 'a', 'b') as C6, ToUpper(a_1.Name) as C7, ToLower(a_1.Name) as C8, SubString(a_1.Name, 4) as C9, SubString(a_1.Name, 6, 3) as C10
	from	Student as a_1
";

            Test("String Trim call", q.Expression, expectedResult);
        }

        [TestMethod]
        public void String_Aggregate_function_test()
        {
            var employees = new Queryable<Employee>(queryProvider);
            var employeeDegrees = new Queryable<EmployeeDegree>(queryProvider);
            var q = from e in employees
                    join ed in employeeDegrees
                                    .GroupBy(x => x.EmployeeId)
                                    .Select(x => new { EmployeeId = x.Key, Degrees = string.Join(", ", x.Select(y => y.Degree)) })
                                    on e.EmployeeId equals ed.EmployeeId
                    select new { e.EmployeeId, e.Name, ed.Degrees };
            string expectedResult = null;
            Test("String Aggregate function test", q.Expression, expectedResult);
        }
    }
}
