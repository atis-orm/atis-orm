using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public class Student
    {
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int? Age { get; set; }

        public DateTime AdmissionDate { get; set; }
        public DateTime RecordCreateDate { get; set; }
        public DateTime? RecordUpdateDate { get; set; }
        public string StudentType { get; set; }
        public string CountryID { get; set; }

        public bool? HasScholarship { get; set; }
    }

    public class StudentExtension : Student
    {
        public bool IsDeleted { get; set; }
    }

    class StudentAttendance
    {
        public Guid RowID { get; set; }
        public string StudentId { get; set; }
        public DateTime? AttendanceDate { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
    }


    public class StudentGrade
    {
        public string RowId { get; set; }
        public string StudentId { get; set; }
        public string Grade { get; set; }

        [NavigationProperty(NavigationType.ToParent, typeof(StudentWithStudentGradeRelation))]
        public Student? NavParentStudent { get; set; }
        [NavigationProperty(NavigationType.ToChildren, typeof(StudentGradeWithStudentGradeDetailRelation))]
        public IQueryable<StudentGradeDetail>? NavStudentGradeDetails { get; set; }
    }

    public class StudentGradeDetail
    {
        public string RowId { get; set; }
        public string StudentGradeRowId { get; set; }
        public string SubjectId { get; set; }
        public int? MarksGained { get; set; }
        public int? TotalMarks { get; set; }
    }

    public class ItemBase
    {
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        [NavigationProperty(NavigationType.ToSingleChild, typeof(ItemBaseWithItemMoreInfoRelation))]
        public Func<ItemMoreInfo> NavItemMoreInfo { get; set; }
        [NavigationProperty(NavigationType.ToSingleChild, typeof(ItemBaseWithItemExtensionRelation))]
        public Func<ItemExtension> NavItemExt { get; set; }
    }

    public class ItemExtension
    {
        public string ItemId { get; set; }
        public decimal? UnitPrice { get; set; }
        [NavigationProperty(NavigationType.ToParent, typeof(ItemBaseWithItemExtensionRelation))]
        public Func<ItemBase>? NavItemBase { get; set; }
        [NavigationProperty(NavigationType.ToChildren, typeof(ItemExtensionWithItemPartRelation))]
        public IQueryable<ItemPart> NavParts { get; set; }

        [NavigationProperty(NavigationType.ToSingleChild, typeof(ItemWithFirstPartRelation))]
        public Func<ItemPart> NavFirstPart { get; set; }
    }

    public class ItemPart
    {
        public Guid RowId { get; set; }
        public string ItemId { get; set; }
        public string PartNumber { get; set; }
    }

    public class ItemMoreInfo
    {
        public string ItemId { get; set; }
        public string TrackingType { get; set; }
    }

    public class Equipment
    {
        public string EquipId { get; set; }
        public string Model { get; set; }
        public string ItemId { get; set; }

        [NavigationProperty(NavigationType.ToParentOptional, typeof(ItemExtensionWithEquipmentRelation))]
        public Func<ItemExtension>? NavItem { get; set; }
    }

    public class Component
    {
        public string CompId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string EquipId { get; set; }
        public string ItemId { get; set; }
        [NavigationProperty(NavigationType.ToParent, typeof(ItemExtensionWithComponentRelation))]
        public ItemExtension? NavItem { get; set; }
    }


    public interface IFullName
    {
        string CalcFullName { get; }
    }

    public class Employee : IFullName
    {
        public Guid RowId { get; set; }
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string ManagerId { get; set; }

        [NavigationProperty(NavigationType.ToChildren, typeof(EmployeeWithEmployeeDegreeRelation))]
        public IQueryable<EmployeeDegree> NavDegrees { get; set; }

        [NavigationProperty(NavigationType.ToChildren, typeof(EmployeeWithEmployeeOnManagerIdRelation))]
        public IQueryable<Employee> NavSubOrdinates { get; set; }
        [NavigationLink(NavigationType.ToParent, nameof(Employee.EmployeeId), nameof(Employee.ManagerId))]
        public Func<Employee> NavManager { get; set; }
        [NavigationProperty(NavigationType.ToChildren, typeof(EmployeeWithEmployeeOnChildrenGrandchildRecursiveRelation))]
        public IQueryable<EmployeeWithTopManagerDto> NavNestedChildren { get; set; }

        [CalculatedProperty(nameof(CalcFullNameExpression))]
        public string CalcFullName => CalcFullNameCompiled(this);
        public static readonly Expression<Func<Employee, string>> CalcFullNameExpression = e => e.Name;
        public static readonly Func<Employee, string> CalcFullNameCompiled = CalcFullNameExpression.Compile();
    }

    public class EmployeeExtension : Employee
    {
        public string Designation { get; set; } = null!;
    }

    public class EmployeeDegree
    {
        public Guid RowId { get; set; }
        public string EmployeeId { get; set; }
        public string Degree { get; set; }
        public string University { get; set; }
        [NavigationProperty(NavigationType.ToChildren, typeof(EmployeeDegreeWithMarksheetRelation))]
        public IQueryable<Marksheet> NavMarksheets { get; set; }
        [NavigationLink(NavigationType.ToParent, nameof(Employee.EmployeeId), nameof(EmployeeDegree.EmployeeId))]
        public Func<Employee> NavEmployee { get; set; }
    }

    public class Marksheet
    {
        public Guid RowId { get; set; }
        public Guid EmployeeDegreeRowId { get; set; }
        public string Course { get; set; }
        public int? TotalMarks { get; set; }
        public int? MarksGained { get; set; }
        public string Grade { get; set; }


        [CalculatedProperty(nameof(CalcPercentageExpression))]
        public decimal? CalcPercentage => CalcPercentageCompiled(this);


        public static readonly Expression<Func<Marksheet, decimal?>> CalcPercentageExpression = 
            m => m.TotalMarks > 0 ? m.MarksGained / m.TotalMarks * 100.0m : 0;


        public static readonly Func<Marksheet, decimal?> CalcPercentageCompiled = CalcPercentageExpression.Compile();
    }

    public class Customer
    {
        public Guid RowId { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        [NavigationProperty(NavigationType.ToChildren, typeof(CustomerWithInvoiceRelation))]
        public IQueryable<Invoice> NavInvoices { get; set; }
    }

    public class Invoice
    {
        public Guid RowId { get; set; }
        public string InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Description { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime? DueDate { get; set; }

        [CalculatedProperty(nameof(CalcInvoiceTotalExpression))]
        public decimal? CalcInvoiceTotal => CalcInvoiceTotalCompiled(this);
        public static readonly Expression<Func<Invoice, decimal?>> CalcInvoiceTotalExpression = i => i.NavLines.Sum(l => l.LineTotal);
        public static readonly Func<Invoice, decimal?> CalcInvoiceTotalCompiled = CalcInvoiceTotalExpression.Compile();

        //[CalculatedProperty(nameof(CalcFirstLineExpression))]
        //public InvoiceDetail? CalcFirstLine => CalcFirstLineCompiled(this).FirstOrDefault();
        //public static readonly Expression<Func<Invoice, IQueryable<InvoiceDetail>>> CalcFirstLineExpression = i => i.NavLines.Top(1);
        //public static readonly Func<Invoice, IQueryable<InvoiceDetail>> CalcFirstLineCompiled = CalcFirstLineExpression.Compile();

        [NavigationProperty(NavigationType.ToParent, typeof(CustomerWithInvoiceRelation))]
        public Func<Customer> NavCustomer { get; set; }
        [NavigationProperty(NavigationType.ToChildren, typeof(InvoiceWithInvoiceDetailRelation))]
        public IQueryable<InvoiceDetail> NavLines { get; set; }

        [NavigationProperty(NavigationType.ToSingleChild, typeof(InvoiceWithInvoiceDetailFirstLineRelation))]
        public Func<InvoiceDetail> NavFirstLine { get; set; }

        [NavigationProperty(NavigationType.ToSingleChild, typeof(InvoiceWithInvoiceDetailTop2LinesRelation))]
        public Func<InvoiceDetail> NavTop2Lines { get; set; }
    }

    public class InvoiceDetail
    {
        public Guid RowId { get; set; }
        public Guid InvoiceId { get; set; }
        public string ItemId { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? Quantity { get; set; }
        public decimal? LineTotal { get; set; }

        [NavigationProperty(NavigationType.ToParent, typeof(InvoiceWithInvoiceDetailRelation))]
        public Func<Invoice> NavInvoice { get; set; }

        [NavigationLink(NavigationType.ToParent, nameof(ItemBase.ItemId), nameof(InvoiceDetail.ItemId))]
        public Func<ItemBase> NavItem { get; set; }
    }

    public class ItemInventoryTransaction
    {
        public Guid RowId { get; set; }
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ItemId { get; set; }
        public string Unit { get; set; }
        public string Comments { get; set; }

        public IQueryable<ItemInventoryTransactionDetail> NavDetailLines { get; set; }

        [NavigationProperty(NavigationType.ToSingleChild, typeof(ItemInventoryTransactionWithInventoryTransactionSummaryRelation))]
        public Func<ItemInventoryTransactionSummary> NavSummaryLine { get; set; }
    }

    public class ItemInventoryTransactionDetail
    {
        public Guid RowId { get; set; }
        public Guid TransactionRowId { get; set; }      // FK
        public string Bin { get; set; }
        public decimal CapturedQty { get; set; }
        public decimal NewQty { get; set; }
        // Captured Line, New Line
        public string LineStatus { get; set; }

        [CalculatedProperty(nameof(CalcQtyGainedExpression))]
        public decimal CalcQtyGained => CalcQtyGainedCompiled(this);
        public static readonly Expression<Func<ItemInventoryTransactionDetail, decimal>> CalcQtyGainedExpression = d => d.NewQty > d.CapturedQty ? d.NewQty - d.CapturedQty : 0;
        public static readonly Func<ItemInventoryTransactionDetail, decimal> CalcQtyGainedCompiled = CalcQtyGainedExpression.Compile();

        [CalculatedProperty(nameof(CalcQtyLostExpression))]
        public decimal CalcQtyLost => CalcQtyLostCompiled(this);
        public static readonly Expression<Func<ItemInventoryTransactionDetail, decimal>> CalcQtyLostExpression = d => d.CapturedQty > d.NewQty ? d.CapturedQty - d.NewQty : 0;
        public static readonly Func<ItemInventoryTransactionDetail, decimal> CalcQtyLostCompiled = CalcQtyLostExpression.Compile();

        [NavigationLink(NavigationType.ToParent, nameof(ItemInventoryTransaction.RowId), nameof(ItemInventoryTransactionDetail.TransactionRowId))]
        public Func<ItemInventoryTransaction> NavParentTransaction { get; set; }
    }

    public class ItemInventoryTransactionSummary
    {
        public Guid TransactionRowId { get; set; }
        public decimal TotalCapturedQty { get; set; }
        public decimal TotalQtyLost { get; set; }
        public decimal TotalQtyGained { get; set; }
    }

    public interface IModelWithItem
    {
        Func<ItemBase> NavItem { get; set; }
    }

    public interface IModelWithSerial
    {
        string SerialNumber { get; set; }
    }


    public class Asset : IModelWithItem, IModelWithSerial
    {
        public Guid RowId { get; set; }
        public string Description { get; set; }
        public string ItemId { get; set; }
        public string SerialNumber { get; set; }
        [NavigationLink(NavigationType.ToParent, nameof(ItemBase.ItemId), nameof(Asset.ItemId))]
        public Func<ItemBase> NavItem { get; set; }
    }

    public class Order
    {
        public string OrderID { get; set; }
        public string CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class OrderDetail
    {
        public string OrderID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

    }

    public class BenchStock_Requisition
    {
        public string DOC_NUM { get; set; }
        public string Trans_DODAAC { get; set; }
        public string Req_PID { get; set; }
        public int Req_Julian { get; set; }
        public int Req_Seq { get; set; }
        public string DocNumDisplay { get; set; }
        public string Status { get; set; }
        public DateTime? SubmittedOn { get; set; }
        public string SubmittedBy { get; set; }
        public string SubmittedByPID { get; set; }
        public DateTime? IssuedOn { get; set; }
        public string IssuedBy { get; set; }
        public string IssuedByPID { get; set; }
        public string Remarks { get; set; }
        public string CreatedByPID { get; set; }
        public string UpdatedByPID { get; set; }
    }

    public class BenchStock_RequisitionLine
    {
        public Guid RowID { get; set; }
        public string BenchStockDocNum { get; set; }
        public string ITEMNMBR { get; set; }
        public string PartNumber { get; set; }
        public int? QtyRequested { get; set; }
        public int? QtyIssued { get; set; }
        public string BSTK_UI { get; set; }
        public int? PD { get; set; }
        public string DMD { get; set; }
        public string JustCodeType { get; set; }
        public string JustCode { get; set; }
        public string Status { get; set; }
        public string IssueDocNum { get; set; }
        public int? IssueDocNumJulian { get; set; }
        public string PBN { get; set; }
        public string UIC { get; set; }
        public string Nomenclature { get; set; }
        public int? QtyAvailable { get; set; }
        public string Bin { get; set; }
        public string BenchStockDocNum_Display { get; set; }
        public string BenchStockDoc_Status { get; set; }
    }

    public class SiteBase 
    {
        public System.String LOCNCODE { get; set; }
        public System.String LOCNDSCR { get; set; }
        public System.String ADDRESS1 { get; set; }
        public System.String ADDRESS2 { get; set; }
        public System.String ADDRESS3 { get; set; }
        public System.String CITY { get; set; }
        public System.String STATE { get; set; }
        public System.String ZIPCODE { get; set; }
        public System.String COUNTRY { get; set; }
        public System.String PHONE1 { get; set; }
        public System.String PHONE2 { get; set; }
        public System.String PHONE3 { get; set; }
        public System.String FAXNUMBR { get; set; }
    }

    public partial class PID_Master
    {
        public System.String PID { get; set; }
        public System.String FNAME { get; set; }
        public System.String MNAME { get; set; }
        public System.String LNAME { get; set; }
        public System.String IDNO { get; set; }
        public System.String RANK { get; set; }
        public System.String ORG { get; set; }
        public System.String LOCN { get; set; }
        public System.DateTime? DOMS { get; set; }
        public System.DateTime? DOP { get; set; }
        public System.String LOCNCODE { get; set; }
        public System.DateTime? DOB { get; set; }
    }

    public class MasterLookup
    {
        public System.String LU_Key { get; set; }
        public System.String LU_Group_Key { get; set; }
        public System.String User_Key { get; set; }
        public System.String LU_Description { get; set; }
        public System.String LU_ShortDesc { get; set; }
    }


    class PlanTable
    {
        public int ID { get; set; }
        public DateTime Date_Time { get; set; }
        public string ID_Plan { get; set; }
    }


    public class Person
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleInitial { get; set; }
        [NavigationLink(NavigationType.ToSingleChild, nameof(Id), nameof(Feet.PersonId))]
        public Func<Feet> NavFeet { get; set; }
        [NavigationLink(NavigationType.ToChildren, nameof(Id), nameof(Shoes.PersonId))]
        public IQueryable<Shoes> NavShoes { get; set; }
    }

    public class Shoes
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public string Style { get; set; }
        public int PersonId { get; set; }
        [NavigationLink(NavigationType.ToParent, nameof(Person.Id), nameof(PersonId))]
        public Person Person { get; set; }
    }

    public class Feet
    {
        public int Id { get; set; }
        public int Size { get; set; }
        public int PersonId { get; set; }
        [NavigationLink(NavigationType.ToParent, nameof(Person.Id), nameof(Id))]
        public Person Person { get; set; }
    }
}
