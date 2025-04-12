using Atis.SqlExpressionEngine.UnitTest.Metadata;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class BinaryToCaseWhenWithinProjectionTests : TestBase
    {
        [TestMethod]
        public void Not_operator_in_projection_expression()
        {
            var studentExtensions = new Queryable<StudentExtension>(queryProvider);
            var q = studentExtensions
                        .Select(x => new { x.StudentId, IsActive = !x.IsDeleted, NoScholarship = !x.HasScholarship });

            string? expectedSql = @"
select	a_1.StudentId as StudentId, 
            case when not (a_1.IsDeleted = 1) then 1 else 0 end as IsActive,
            case when not (a_1.HasScholarship = 1) then 1 else 0 end as NoScholarship
	from	StudentExtension as a_1
";

            Test("Not operator in projection expression", q.Expression, expectedSql);
        }

        [TestMethod]
        public void Select_direct_binary_expression()
        {
            var students = new Queryable<StudentExtension>(queryProvider);
            var q = students.Select(x => x.Age > 18);

            string? expectedSql = @"
select	case when (a_1.Age > 18) then 1 else 0 end as Col1
	from	StudentExtension as a_1
";

            Test("Select direct binary expression", q.Expression, expectedSql);
        }

        [TestMethod]
        public void Select_projection_rewrites_binary_comparison()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Select(x => new
            {
                x.StudentId,
                IsAdult = x.Age >= 18
            });

            string? expectedSql = @"
select	a_1.StudentId as StudentId, case when (a_1.Age >= 18) then 1 else 0 end as IsAdult
	from	Student as a_1
";

            Test("Select projection rewrites binary comparison", q.Expression, expectedSql);
        }

        [TestMethod]
        public void Select_projection_with_logical_and_and_binary_subexpr()
        {
            var students = new Queryable<StudentExtension>(queryProvider);
            var q = students.Select(x => new
            {
                x.StudentId,
                IsAllowed = x.Age > 18 && (x.HasScholarship ?? false)
            });

            string? expectedSql = @"
select	a_1.StudentId as StudentId, case when ((a_1.Age > 18) and (isnull(a_1.HasScholarship, 0) = 1)) then 1 else 0 end as IsAllowed
	from	StudentExtension as a_1
";

            Test("Select projection with logical and + binary subexpr", q.Expression, expectedSql);
        }

        [TestMethod]
        public void Nested_ternary_with_binary_comparisons_is_handled()
        {
            var students = new Queryable<StudentExtension>(queryProvider);
            var q = students.Select(x => new
            {
                x.StudentId,
                Flag = x.StudentType == "Open" ? x.Age > 18 : x.Age > 10
            });

            string? expectedSql = @"
select	a_1.StudentId as StudentId, case when (a_1.StudentType = 'Open') then 
                                        case when (a_1.Age > 18) then 1 else 0 end 
                                    else 
                                        case when (a_1.Age > 10) then 1 else 0 end 
                                    end as Flag
	from	StudentExtension as a_1
";

            Test("Nested ternary with binary comparisons is handled", q.Expression, expectedSql);
        }

        [TestMethod]
        public void Coalesce_expression_wraps_inner_binary_correctly()
        {
            var students = new Queryable<StudentExtension>(queryProvider);
            bool? outsideVariable = null;
            var q = students.Select(x => new
            {
                x.StudentId,
                Flag = outsideVariable ?? (x.HasScholarship ?? (x.Age > 10))
            });

            string? expectedSql = @"
select	a_1.StudentId as StudentId, isnull(null, isnull(a_1.HasScholarship, case when (a_1.Age > 10) then 1 else 0 end)) as Flag
	from	StudentExtension as a_1
";

            Test("Coalesce expression wraps inner binary correctly", q.Expression, expectedSql);
        }
    }
}
