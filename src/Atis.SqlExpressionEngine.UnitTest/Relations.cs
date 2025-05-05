using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest
{

    public class StudentWithStudentGradeRelation : EntityRelation<Student, StudentGrade>
    {
        public override Expression<Func<Student, StudentGrade, bool>> JoinExpression
            => (s, sg) => s.StudentId == sg.StudentId;
    }

    public class StudentGradeWithStudentGradeDetailRelation : EntityRelation<StudentGrade, StudentGradeDetail>
    {
        public override Expression<Func<StudentGrade, StudentGradeDetail, bool>> JoinExpression
            => (sg, sgd) => sg.RowId == sgd.StudentGradeRowId;
    }

    public class ItemBaseWithItemExtensionRelation : EntityRelation<ItemBase, ItemExtension>
    {
        public override Expression<Func<ItemBase, ItemExtension, bool>> JoinExpression
            => (ib, ie) => ib.ItemId == ie.ItemId;
    }

    public class ItemExtensionWithEquipmentRelation : EntityRelation<ItemExtension, Equipment>
    {
        public override Expression<Func<ItemExtension, Equipment, bool>> JoinExpression
            => (ie, e) => ie.ItemId == e.ItemId;
    }

    public class ItemExtensionWithComponentRelation : EntityRelation<ItemExtension, Component>
    {
        public override Expression<Func<ItemExtension, Component, bool>> JoinExpression
            => (ie, c) => ie.ItemId == c.ItemId;
    }

    public class ItemBaseWithItemMoreInfoRelation : EntityRelation<ItemBase, ItemMoreInfo>
    {
        public override Expression<Func<ItemBase, ItemMoreInfo, bool>> JoinExpression
            => (ib, imi) => ib.ItemId == imi.ItemId;
    }

    public class ItemExtensionWithItemPartRelation : EntityRelation<ItemExtension, ItemPart>
    {
        public override Expression<Func<ItemExtension, ItemPart, bool>> JoinExpression 
            => (ie, ip) => ie.ItemId == ip.ItemId;
    }


    public class ItemWithFirstPartRelation : EntityRelation<ItemExtension, ItemPart>
    {
        // JoinExpression null will make it outer apply
        public override Expression<Func<ItemExtension, ItemPart, bool>>? JoinExpression => null;
        public override Expression<Func<ItemExtension, IQueryable<ItemPart>>>? FromParentToChild(IQueryProvider queryProvider)
        {
            return parent => parent.NavParts.Take(1);
        }
    }


    public class EmployeeWithEmployeeDegreeRelation : EntityRelation<Employee, EmployeeDegree>
    {
        public override Expression<Func<Employee, EmployeeDegree, bool>> JoinExpression
            => (e, ed) => e.EmployeeId == ed.EmployeeId;
    }

    public class EmployeeWithEmployeeOnManagerIdRelation : EntityRelation<Employee, Employee>
    {
        public override Expression<Func<Employee, Employee, bool>> JoinExpression
            => (e, e2) => e.EmployeeId == e2.ManagerId;
    }

    public class EmployeeDegreeWithMarksheetRelation : EntityRelation<EmployeeDegree, Marksheet>
    {
        public override Expression<Func<EmployeeDegree, Marksheet, bool>> JoinExpression
            => (ed, m) => ed.RowId == m.EmployeeDegreeRowId;
    }

    public class CustomerWithInvoiceRelation : EntityRelation<Customer, Invoice>
    {
        public override Expression<Func<Customer, Invoice, bool>> JoinExpression
            => (c, i) => c.RowId == i.CustomerId;
    }

    public class InvoiceWithInvoiceDetailRelation : EntityRelation<Invoice, InvoiceDetail>
    {
        public override Expression<Func<Invoice, InvoiceDetail, bool>> JoinExpression
            => (i, id) => i.RowId == id.InvoiceId;
    }

    public class ItemInventoryTransactionWithInventoryTransactionDetailRelation : EntityRelation<ItemInventoryTransaction, ItemInventoryTransactionDetail>
    {
        public override Expression<Func<ItemInventoryTransaction, ItemInventoryTransactionDetail, bool>> JoinExpression
            => (iit, itd) => iit.RowId == itd.TransactionRowId;
    }

    public class ItemInventoryTransactionWithInventoryTransactionSummaryRelation : EntityRelation<ItemInventoryTransaction, ItemInventoryTransactionSummary>
    {
        public override Expression<Func<ItemInventoryTransaction, ItemInventoryTransactionSummary, bool>> JoinExpression
            => (iit, its) => iit.RowId == its.TransactionRowId;

        public override Expression<Func<ItemInventoryTransaction, IQueryable<ItemInventoryTransactionSummary>>>? FromParentToChild(IQueryProvider queryProvider)
        {
            var q = new Queryable<ItemInventoryTransactionDetail>(queryProvider);
            return parent => q.GroupBy(x => x.TransactionRowId)
                                       .Select(x => new ItemInventoryTransactionSummary
                                       {
                                           TransactionRowId = x.Key,
                                           TotalCapturedQty = x.Sum(y => y.CapturedQty),
                                           TotalQtyGained = x.Sum(y => y.CalcQtyGained),
                                           TotalQtyLost = x.Sum(y => y.CalcQtyLost),
                                       });
        }
    }

    public class InvoiceWithInvoiceDetailFirstLineRelation : EntityRelation<Invoice, InvoiceDetail>
    {
        // JoinExpression null will make it outer apply
        public override Expression<Func<Invoice, InvoiceDetail, bool>>? JoinExpression => null;
        public override Expression<Func<Invoice, IQueryable<InvoiceDetail>>>? FromParentToChild(IQueryProvider queryProvider)
        {
            return parent => parent.NavLines.Take(1);
        }
    }

    public class InvoiceWithInvoiceDetailTop2LinesRelation : EntityRelation<Invoice, InvoiceDetail>
    {
        // JoinExpression null will make it outer apply
        public override Expression<Func<Invoice, InvoiceDetail, bool>>? JoinExpression => null;
        public override Expression<Func<Invoice, IQueryable<InvoiceDetail>>>? FromParentToChild(IQueryProvider queryProvider)
        {
            return parent => parent.NavLines.Take(2);
        }
    }

    public class EmployeeWithEmployeeOnChildrenGrandchildRecursiveRelation : EntityRelation<Employee, EmployeeWithTopManagerDto>
    {
        public override Expression<Func<Employee, EmployeeWithTopManagerDto, bool>> JoinExpression
            => (e, e2) => e.EmployeeId == e2.TopManagerId;
        public override Expression<Func<Employee, IQueryable<EmployeeWithTopManagerDto>>>? FromParentToChild(IQueryProvider queryProvider)
        {
            return parent => parent.NavSubOrdinates
                                    .RecursiveUnion(anchorSource => anchorSource.SelectMany(relationManager => relationManager.NavSubOrdinates))
                                    .Select(childGrandChild => new EmployeeWithTopManagerDto { EmployeeId = childGrandChild.EmployeeId, ImmediateManagerId = childGrandChild.ManagerId, TopManagerId = parent.EmployeeId });
            //throw new NotImplementedException();
        }
    }

    public class EmployeeWithTopManagerDto
    {
        public string? EmployeeId { get; set; }
        public string? ImmediateManagerId { get; set; }
        public string? TopManagerId { get; set; }

        [NavigationLink(NavigationType.ToParent, nameof(Employee.EmployeeId), nameof(EmployeeId))]
        public Func<Employee> NavEmployee { get; set; }
    }
}
