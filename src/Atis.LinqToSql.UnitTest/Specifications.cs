using Atis.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{
    public class StudentIsAdultSpecification : ExpressionSpecificationBase<Student>
    {
        public override Expression<Func<Student, bool>> ToExpression()
        {
            return student => student.Age >= 18;
        }
    }

    public class InvoiceIsDueOnGivenDateSpecification : ExpressionSpecificationBase<Invoice>
    {
        public InvoiceIsDueOnGivenDateSpecification(DateTime? givenDate)
        {
            this.GivenDate = givenDate;
        }

        public DateTime? GivenDate { get; }

        public override Expression<Func<Invoice, bool>> ToExpression()
        {
            return invoice => invoice.DueDate >= this.GivenDate;
        }
    }

    public class CustomerIsInvalidSpecification : ExpressionSpecificationBase<Customer>
    {
        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return customer => customer.Status == "Disabled" || customer.Status == "Blocked";
        }
    }
}
