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
    }
}
