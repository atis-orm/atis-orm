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

            string expectedSql = @"
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

            string expectedSql = @"
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

            string expectedSql = @"
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

            string expectedSql = @"
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

            string expectedSql = @"
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

            string expectedSql = @"
select	a_1.StudentId as StudentId, isnull(null, isnull(a_1.HasScholarship, case when (a_1.Age > 10) then 1 else 0 end)) as Flag
	from	StudentExtension as a_1
";

            Test("Coalesce expression wraps inner binary correctly", q.Expression, expectedSql);
        }

        [TestMethod]
        public void String_in_compare_with_boolean_variable_should_translate_to_case_when()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Select(x => new { Flag = new[] { "Type1", "Type2" }.Contains(x.StudentType) == true });
            string expectedResult = @"
select	case when (case when a_1.StudentType in ('Type1', 'Type2') then 1 else 0 end = 1) then 1 else 0 end as Flag
	from	Student as a_1
";

            Test("String in compare with boolean variable should translate to case when", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Not_on_string_in_used_in_Select()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students.Select(x => new { Flag = !(new[] { "Type1", "Type2" }.Contains(x.StudentType)) });
            string expectedResult = @"
select	case when not a_1.StudentType in ('Type1', 'Type2') then 1 else 0 end as Flag
	from	Student as a_1
";

            Test("String in compare with boolean variable should translate to case when", q.Expression, expectedResult);
        }

        [TestMethod()]
        public void Logical_expression_compared_with_true_false_in_projection_should_remove_true_false()
        {
            var students = new Queryable<StudentExtension>(queryProvider);
            var q = students.Select(x => new { x.StudentId, x.Name, NotEndsWith123 = x.Name.EndsWith("123") == false });
            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, case when (case when (a_1.Name like '%' + '123') then 1 else 0 end = 0) then 1 else 0 end as NotEndsWith123
	from	StudentExtension as a_1
";

            Test("Logical expression compared with true/false in projection should remove true/false", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Any_equal_to_other_boolean_field()
        {
            var students = new Queryable<StudentExtension>(queryProvider);
            var studentAttendance = new Queryable<StudentAttendance>(queryProvider);
            var q = students.Select(x => new { x.StudentId, Flag = studentAttendance.Where(y => y.StudentId == x.StudentId).Any() == x.IsDeleted });
            string expectedResult = @"
select	a_1.StudentId as StudentId, case when (case when exists(
		select	1 as Col1
		from	StudentAttendance as a_2
		where	(a_2.StudentId = a_1.StudentId)
	) then 1 else 0 end = a_1.IsDeleted) then 1 else 0 end as Flag
	from	StudentExtension as a_1
";

            Test("Any equal to other boolean field", q.Expression, expectedResult);
        }
    }
}
