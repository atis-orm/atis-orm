using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class OrderByTests : TestBase
    {

        [TestMethod]
        public void LambdaParameter_in_OrderBy()
        {
            var q = new Queryable<Student>(dbc).Select(x => x.StudentId).OrderBy(t => t).OrderBy(u => u + "123");
            var expectedResult = @"
select  a_1.StudentId as Col1
from    Student as a_1
order by Col1 asc, (a_1.StudentId + '123') asc";
            Test("LambdaParameter in OrderBy", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Multiple_OrderBy_calls_with_LambdaParameter()
        {
            var q = new Queryable<Student>(dbc)
                                    .Select(x => x.StudentId)
                                    .Where(x => x == "5")
                                    .OrderBy(x => x + "3")
                                    .OrderByDescending(x => x)
                                    .Take(5);
            var expectedResult = @"
select  top (5) a_2.Col1 as Col1
from    (
        select  a_1.StudentId as Col1
        from    Student as a_1
) as a_2
where   (a_2.Col1 = '5')
order by (a_2.Col1 + '3') asc, a_2.Col1 desc";
            Test("Complex Order By Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Expression_in_OrderBy()
        {
            var q = new Queryable<Student>(dbc)
                .Select(x => new { Id = x.StudentId })
                .OrderBy(x => x.Id + "3")
                .OrderByDescending(x => x.Id)
                .Take(5);
            var expectedResult = @"
select	top (5)	a_1.StudentId as Id
from	Student as a_1
order by (a_1.StudentId + '3') asc, Id desc";
            Test("Expression In OrderBy Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void ComputedColumn_Select_Then_OrderBy_Test()
        {
            var invoiceDetails = new Queryable<InvoiceDetail>(dbc);

            var q = invoiceDetails
                .Select(e => new { e.InvoiceId, LineTotal = e.UnitPrice * e.Quantity })
                .OrderBy(x => x.LineTotal);

            string? expectedResult = @"    
	select	a_1.InvoiceId as InvoiceId, (a_1.UnitPrice * a_1.Quantity) as LineTotal
	from	InvoiceDetail as a_1
	order by LineTotal asc
";

            Test("Select with computed column followed by OrderBy should wrap correctly", q.Expression, expectedResult);
        }

    }
}
