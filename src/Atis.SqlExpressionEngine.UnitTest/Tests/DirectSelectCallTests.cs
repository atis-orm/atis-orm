﻿namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class DirectSelectCallTests : TestBase
    {
        [TestMethod]
        public void Direct_Select_method_call()
        {
            var q = queryProvider.Select(() => new { n = 1 })
                            .Where(x => x.n > 5);
            string expectedResult = @"
select	a_1.n as n
from	(
	select	1 as n
) as a_1
where	(a_1.n > 5)
";
            Test("Direct Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Direct_Select_method_call_with_scalar_value()
        {
            var q = queryProvider.Select(() => 1)
                            .Where(x => x > 5);
            string expectedResult = @"
select	a_1.Col1 as Col1
	from	(
		select	1 as Col1
	) as a_1
	where	(a_1.Col1 > 5)
";
            Test("Direct Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Direct_Select_method_call_with_sub_query_as_scalar_column()
        {
            var invoices = new Queryable<Invoice>(this.queryProvider);
            var q = queryProvider.Select(() => invoices.Count())
                            .Where(x => x > 5);
            string expectedResult = @"
select	a_2.Col1 as Col1
from	(
	select	(
		select	Count(1) as Col1
		from	Invoice as a_1
	) as Col1
) as a_2
where	(a_2.Col1 > 5)
";
            Test("Direct Select method call with sub query as scalar column", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Direct_Select_method_call_with_sub_query_as_projection()
        {
            var invoices = new Queryable<Invoice>(this.queryProvider);
            var q = queryProvider.Select(() => new { InvoiceCount = invoices.Count() })
                            .Where(x => x.InvoiceCount > 5);
            string expectedResult = @"
select	a_2.InvoiceCount as InvoiceCount
from	(
	select	(
		select	Count(1) as Col1
		from	Invoice as a_1
	) as InvoiceCount
) as a_2
where	(a_2.InvoiceCount > 5)
";
            Test("Direct Select method call with sub query as projection", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Direct_Select_with_recursive_query_to_generate_sequence()
        {
            var q = queryProvider.Select(() => new { n = 1 })
                           .RecursiveUnion(
                                anchor => anchor
                                            .Where(anchorMember => anchorMember.n < 10)
                                            .Select(anchorMember => new { n = anchorMember.n + 1 })
                            );
            string expectedResult = @"
with cte_1 as
(
	select a_2.n as n
	from (select 1 as n) as a_2
	union all
	select (a_3.n + 1) as n
	from cte_1 as a_3
	where (a_3.n < 10)
)
select a_4.n as n
from cte_1 as a_4
";
            Test("Direct Select with recursive query to generate sequence", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Direct_Select_with_recursive_query_generate_missing_dates()
        {
            var start = new DateTime(2024, 1, 1);
            var end = new DateTime(2024, 1, 31);
            var days = (end - start).Days;

            var dateSequence = queryProvider.Select(() => new { DayOffset = 0 })
                                    .RecursiveUnion(anchor =>
                                        anchor
                                            .Where(x => x.DayOffset < days)
                                            .Select(x => new { DayOffset = x.DayOffset + 1 })
                                    )
                                    .Select(x => new { Date = start.AddDays(x.DayOffset) });

            var invoices = new Queryable<Invoice>(this.queryProvider);
            var q = from date in dateSequence
                    join invoice in invoices on date.Date equals invoice.InvoiceDate into invoiceGroup
                    select new { date.Date, InvoiceCount = invoiceGroup.Count(), TotalSales = invoiceGroup.SelectMany(x => x.NavLines).Sum(y => y.LineTotal) };

            string expectedResult = @"
with cte_1 as
	(
		select a_2.DayOffset as DayOffset
		from (select 0 as DayOffset) as a_2
		union all
		select (a_3.DayOffset + 1) as DayOffset
		from cte_1 as a_3
		where (a_3.DayOffset < 30)
	)
	select a_5.Date as Date, (
			select Count(1) as Col1
			from Invoice as a_6
			where (a_5.Date = a_6.InvoiceDate)
		) as InvoiceCount, (
			select Sum(NavLines_8.LineTotal) as Col1
			from Invoice as a_7
					inner join InvoiceDetail as NavLines_8 on (a_7.RowId = NavLines_8.InvoiceId)
			where (a_5.Date = a_7.InvoiceDate)
		) as TotalSales
	from (
			select dateAdd(Day, a_4.DayOffset, '2024-01-01 00:00:00') as Date
			from cte_1 as a_4
		) as a_5
";
            Test("Direct Select with recursive query to generate missing dates", q.Expression, expectedResult);
        }
    }
}
