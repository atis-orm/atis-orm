using Atis.SqlExpressionEngine.UnitTest.Metadata;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class BooleanValueUsageWithinPredicateTests : TestBase
    {
        [TestMethod]
        public void Not_operator_on_boolean_field_in_where()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions
                        .Where(x => !x.IsDeleted && !(x.HasScholarship ?? false))
                        .Select(x => new { x.StudentId, x.Name });

            string? expectedSql = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
	from	StudentExtension as a_1
	where	(not (a_1.IsDeleted = 1) and not (isnull(a_1.HasScholarship, 0) = 1))
";

            Test("Not operator on boolean field in where", q.Expression, expectedSql);
        }


        [TestMethod]
        public void Direct_boolean_field_selected_in_Where_predicate()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Where(x => x.HasScholarship ?? false).Select(x => new { x.StudentId, x.Name });
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
	from	Student as a_1
	where	(isnull(a_1.HasScholarship, 0) = 1)
";
            Test("Direct boolean field selected in Where predicate", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Sql_function_usage_within_linq_expression()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students
                        .Where(x => x.CountryID == "123" && IsAsianCountry(x.CountryID, x.Age > 18))
                        .Select(x => new { x.StudentId, x.Name, IsAsianCountry = IsAsianCountry(x.CountryID, !(x.Age > 18)) });
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, dbo.FUNC_IS_ASIAN_COUNTRY(a_1.CountryID, case when not (a_1.Age > 18) then 1 else 0 end) as IsAsianCountry
	from	Student as a_1
	where	((a_1.CountryID = '123') and (dbo.FUNC_IS_ASIAN_COUNTRY(a_1.CountryID, case when (a_1.Age > 18) then 1 else 0 end) = 1))
";
            Test("Sql function usage within linq expression", q.Expression, expectedResult);
        }

        [SqlFunction("dbo.FUNC_IS_ASIAN_COUNTRY")]
        public static bool IsAsianCountry(string? countryId, bool useDefault)
        {
            throw new NotImplementedException();
        }


        [TestMethod]
        public void Where_clause_rewrites_nullable_bool_to_equal_true()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);            
            var q = studentExtensions.Where(x => x.HasScholarship ?? false).Select(x=> new { x.StudentId, x.Name });
            string? expectedResult = @"
select  a_1.StudentId as StudentId, a_1.Name as Name
from    StudentExtension as a_1
where   (isnull(a_1.HasScholarship, 0) = 1)
";

            Test("Where clause rewrites nullable bool to equal true", q.Expression, expectedResult);
        }


        [TestMethod]
        public void OrElse_expression_rewrites_left_side()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions.Where(x => (x.HasScholarship ?? false) || x.IsDeleted == false).Select(x => new { x.StudentId, x.Name });
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
	from	StudentExtension as a_1
	where	((isnull(a_1.HasScholarship, 0) = 1) or (a_1.IsDeleted = 0))
";

            Test("OrElse expression rewrites left side", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Select_projection_does_not_rewrite()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions.Select(x => new { Flag = x.IsDeleted });

            string? expectedResult = @"
select	a_1.IsDeleted as Flag
	from	StudentExtension as a_1
";

            Test("Select projection does not rewrite", q.Expression, expectedResult);
        }


        [TestMethod]
        public void OrderBy_does_not_rewrite()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions.OrderBy(x => x.HasScholarship).Select(x => new { x.StudentId, x.Name, x.IsDeleted }).OrderBy(x => x.IsDeleted);

            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.IsDeleted as IsDeleted
	from	StudentExtension as a_1
	order by a_1.HasScholarship asc, IsDeleted asc
";

            Test("OrderBy does not rewrite", q.Expression, expectedResult);
        }


        [TestMethod]
        public void GroupBy_does_not_rewrite()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions.GroupBy(x => x.IsDeleted);

            string? expectedResult = @"
select	a_1.IsDeleted as Col1
	from	StudentExtension as a_1
	group by a_1.IsDeleted
";

            Test("GroupBy does not rewrite", q.Expression, expectedResult);
        }

        [TestMethod]
        public void ConditionalExpression_test_is_rewritten()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions.Select(x => new { x.StudentId, x.Name, IsDeleted = x.IsDeleted ? "Valid" : "Invalid" });

            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, case when (a_1.IsDeleted = 1) then 'Valid' else 'Invalid' end as IsDeleted
	from	StudentExtension as a_1
";
            Test("ConditionalExpression test is rewritten", q.Expression, expectedResult);
        }
    }
}
