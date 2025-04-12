namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class DateFunctionsTests : TestBase
    {
        [TestMethod]
        public void Date_properties_test()
        {
            var date = new DateTime(2024, 1, 1, 10, 5, 30);
            var q = this.queryProvider.Select(() => new { date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, date.Ticks });
            string? expectedResult = @"
select	datePart(Year, '2024-01-01 10:05:30') as Year, 
        datePart(Month, '2024-01-01 10:05:30') as Month, 
        datePart(Day, '2024-01-01 10:05:30') as Day, 
        datePart(Hour, '2024-01-01 10:05:30') as Hour, 
        datePart(Minute, '2024-01-01 10:05:30') as Minute, 
        datePart(Second, '2024-01-01 10:05:30') as Second, 
        datePart(Millisecond, '2024-01-01 10:05:30') as Millisecond, 
        datePart(Tick, '2024-01-01 10:05:30') as Ticks
";

            Test("Date properties test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Date_add_test()
        {
            var date = new DateTime(2024, 1, 1, 10, 5, 30);
            var q = this.queryProvider.Select(() => new { Y = date.AddYears(1), M = date.AddMonths(1), D = date.AddDays(1), H = date.AddHours(1), MN = date.AddMinutes(1), S = date.AddSeconds(1), MS = date.AddMilliseconds(1), NS = date.AddTicks(1) });
            string? expectedResult = @"
select	dateAdd(Year, 1, '2024-01-01 10:05:30') as Y, dateAdd(Month, 1, '2024-01-01 10:05:30') as M, dateAdd(Day, 1, '2024-01-01 10:05:30') as D, dateAdd(Hour, 1, '2024-01-01 10:05:30') as H, dateAdd(Minute, 1, '2024-01-01 10:05:30') as MN, dateAdd(Second, 1, '2024-01-01 10:05:30') as S, dateAdd(Millisecond, 1, '2024-01-01 10:05:30') as MS, dateAdd(Tick, 1, '2024-01-01 10:05:30') as NS
";

            Test("Date add test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Select_nullable_DateTime_property_with_Date_property_selected_should_translate_to_cast()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Select(x => x.RecordUpdateDate.Value.Date);
            string? expectedResult = @"
select	cast(a_1.RecordUpdateDate as Date) as Col1
	from	Student as a_1
";
            
            Test("Select nullable DateTime property with Date property selected should translate to cast", q.Expression, expectedResult);
        }

    }
}
