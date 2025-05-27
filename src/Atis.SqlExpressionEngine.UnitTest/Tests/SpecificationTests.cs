namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class SpecificationTests : TestBase
    {

        [TestMethod]
        public void Specification_instance_created_before_query()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var specification = new StudentIsAdultSpecification();
            var q = students.Where(x => specification.IsSatisfiedBy(x));
            string expectedResult = @"select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Age >= 18)";
            Test("Specification Pre Initialized Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Specification_instance_created_within_query()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => new StudentIsAdultSpecification().IsSatisfiedBy(x));
            string expectedResult = @"select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Age >= 18)";
            Test("Specification Inline Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Specification_with_parameter_outer_query_LambdaParameter_with_column_passed_in_specification_constructor_should_replace_Specification_expression_property_with_outer_query_column()
        {
            var invoices = new Queryable<Invoice>(new QueryProvider());

            var q = invoices.Where(x => new InvoiceIsDueOnGivenDateSpecification(x.InvoiceDate).IsSatisfiedBy(x))
                              .Where(x => !new CustomerIsInvalidSpecification().IsSatisfiedBy(x.NavCustomer()));

            string expectedResult = @"
select	a_1.RowId as RowId, a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate, a_1.Description as Description, a_1.CustomerId as CustomerId, a_1.DueDate as DueDate
	from	Invoice as a_1
		inner join Customer as NavCustomer_2 on (NavCustomer_2.RowId = a_1.CustomerId)
	where	(a_1.DueDate >= a_1.InvoiceDate) and not ((NavCustomer_2.Status = 'Disabled') or (NavCustomer_2.Status = 'Blocked'))
";
            Test("Specification With Constructor Arguments Test", q.Expression, expectedResult);
        }

    }
}
