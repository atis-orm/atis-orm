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
    }
}
