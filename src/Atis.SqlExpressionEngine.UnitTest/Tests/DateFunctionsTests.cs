namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class DateFunctionsTests : TestBase
    {
        [TestMethod]
        public void Date_properties_test()
        {
            var date = new DateTime(2024, 1, 1, 10, 5, 30);
            var q = this.queryProvider.Select(() => new { date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond });
            string? expectedResult = @"
select	datepart(year, '2024-01-01 10:05:30') as Year, datepart(month, '2024-01-01 10:05:30') as Month, datepart(day, '2024-01-01 10:05:30') as Day, 
        datepart(hour, '2024-01-01 10:05:30') as Hour, datepart(minute, '2024-01-01 10:05:30') as Minute, 
        datepart(second, '2024-01-01 10:05:30') as Second, datepart(millisecond, '2024-01-01 10:05:30') as Millisecond
";

            Test("Date properties test", q.Expression, expectedResult);
        }
    }
}
