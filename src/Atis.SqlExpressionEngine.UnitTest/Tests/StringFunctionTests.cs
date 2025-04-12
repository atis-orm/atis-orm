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
            string? expectedResult = @"select	Concat('1', 2, '3') as C1, Concat('abc', 'def') as C2";

            Test("String concat method test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void ToString_method_should_translate_to_cast_test()
        {
            var q = queryProvider.Select(() => new { C1 = 123.ToString(), C2 = 123.456.ToString() });
            string? expectedResult = @"select	cast(123 as NonUnicodeString(max)) as C1, cast(123.456 as NonUnicodeString(max)) as C2";
            Test("ToString method test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void String_compare()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Where(x => string.Compare(x.Name, "A") > 0);
            string? expectedResult = @"
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
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Name >= 'A')
";
            Test("String CompareTo", q.Expression, expectedResult);
        }
    }
}
