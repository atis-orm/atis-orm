namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class CalculatedPropertyTests : TestBase
    {

        [TestMethod]
        public void Calculated_property_defined_on_interface_with_query_performed_on_interface()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<IFullName> q1 = employees;
            var q = q1.Where(x => x.CalcFullName.Contains("Abc"));
            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	(a_1.Name like '%' + 'Abc' + '%')
";

            Test("Calculated Property in Interface Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Calculated_property_in_Where_test()
        {
            var marksheets = new Queryable<Marksheet>(new QueryProvider());

            var q = marksheets.Where(x => x.CalcPercentage > 50).Select(x => new { x.Course, x.Grade });

            string expectedResult = @"
    select	a_1.Course as Course, a_1.Grade as Grade
	from	Marksheet as a_1
	where	(case when (a_1.TotalMarks > 0) then ((a_1.MarksGained / a_1.TotalMarks) * 100.0) else 0 end > 50)
";
            Test("Calculated Property Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Multiple_children_navigation_used_in_calculated_property()
        {
            var invoices = new Queryable<Invoice>(new QueryProvider());
            // below Calculated property contains multiple children navigation
            var q = invoices.Where(x => x.CalcInvoiceTotal > 100).Select(x => new { x.InvoiceId, x.InvoiceDate });
            string expectedResult = @"
select	a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate
	from	Invoice as a_1
	where	((
		select	Sum(a_2.LineTotal) as Col1
		from	InvoiceDetail as a_2
		where	(a_1.RowId = a_2.InvoiceId)
	) > 100)
";
            Test("Calculated Property With Navigation Test", q.Expression, expectedResult);
        }



    }
}
