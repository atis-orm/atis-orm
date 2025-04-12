namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class PagingTests : TestBase
    {

        [TestMethod]
        public void Paging_query_extension_method_test()
        {
            IQueryable<Student> students = new Queryable<Student>(new QueryProvider());

            var q = students.Where(x => x.Address.Contains("City"))
                            .OrderBy(x => x.Name)
                            .Select(x => new { x.Name, Id = x.StudentId })
                            .Paging(2, 10);

            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like '%' + 'City' + '%')
order by a_1.Name asc
offset 10 rows fetch next 10 rows only";
            Test("Paging Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Paging_using_Skip_and_Take_methods()
        {
            int pageNumber = 5;
            int pageSize = 10;
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .OrderBy(x => x.Name)
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like '%' + 'City' + '%')
order by a_1.Name asc
offset 40 rows fetch next 10 rows only";
            Test("Paging With Skip And Take Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Paging_query_extension_method_without_OrderBy_test()
        {
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .Paging(2, 10)
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like '%' + 'City' + '%')
order by 1 asc
offset 10 rows fetch next 10 rows only";
            Test("Paging Without OrderBy Test", q.Expression, expectedResult);
        }

    }
}