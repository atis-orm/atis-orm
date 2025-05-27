namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class InValuesTests : TestBase
    {

        [TestMethod]
        public void Direct_array_variable_Contains_should_be_translated_to_In_SQL_operator()
        {
            var values = new[] { "HR", "Finance" };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => values.Contains(x.Department));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR','Finance')
";

            Test("Direct array variable Contains should be translated to In SQL operator", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Inline_array_Contains_should_be_translated_to_In_SQL_operator()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => new[] { "HR", "Finance" }.Contains(x.Department));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR', 'Finance')
";

            Test("Inline array Contains should be translated to In SQL operator", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Nested_object_member_Contains_should_be_translated_to_In_SQL_operator()
        {
            var obj = new { Departments = new[] { "HR", "Finance" } };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => obj.Departments.Contains(x.Department));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR','Finance')
";

            Test("Nested object member Contains should be translated to In SQL operator", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Inline_array_Any_Y_Equals_X_should_be_translated_to_In_SQL_operator()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => new[] { "HR", "Finance" }.Any(y => y == x.Department));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR', 'Finance')
";
            Test("Inline array Any Y Equals X should be translated to In SQL operator", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Nested_object_member_Any_Y_Equals_X_should_be_translated_to_In_SQL_operator()
        {
            var obj = new { RowIds = new[] { new Guid("34878994-7241-4ebb-870f-186a9494f7c1"), new Guid("6a8844fa-66ba-4319-8b8a-d9b03787b512") } };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => obj.RowIds.Any(y => y == x.RowId));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.RowId in ('34878994-7241-4ebb-870f-186a9494f7c1','6a8844fa-66ba-4319-8b8a-d9b03787b512')
";

            Test("Nested object member Any Y Equals X should be translated to In SQL operator", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Direct_array_variable_Any_Y_Equals_X_should_be_translated_to_In_SQL_operator()
        {
            var departments = new[] { "HR", "Finance" };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => departments.Any(y => y == x.Department));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR','Finance')
";

            Test("Direct array variable Any Y Equals X should be translated to In SQL operator", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Nested_object_member_Any_X_Equals_Y_should_be_translated_to_In_SQL_operator()
        {
            var obj = new { Departments = new[] { "HR", "Finance" } };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => obj.Departments.Any(y => x.Department == y));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR','Finance')
";

            Test("Nested object member Any X Equals Y should be translated to In SQL operator", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Direct_array_variable_Any_X_Equals_Y_should_be_translated_to_In_SQL_operator()
        {
            var departments = new[] { "HR", "Finance" };
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => departments.Any(y => x.Department == y));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	a_1.Department in ('HR','Finance')
";

            Test("Direct array variable Any X Equals Y should be translated to In SQL operator", q.Expression, expectedResult);
        }
    }
}
