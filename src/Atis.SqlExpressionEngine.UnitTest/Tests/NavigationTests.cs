using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class NavigationTests : TestBase
    {

        [TestMethod]
        public void Single_navigation_property_multiple_times_used()
        {
            Expression<Func<object>> temp = () =>
            queryProvider.DataSet<Equipment>()
            .Where(x => x.NavItem().UnitPrice > 500)
            .Select(x => new { x.NavItem().ItemId, x.NavItem().UnitPrice, x.EquipId })
            //.Select(x => new { ItemId = LinqToSql.QueryExtensions.Nav<Equipment, ItemExtension, decimal?>(x, "NavItemId", dbc.DataSet<ItemExtension>(), param0 => param0.UnitPrice, SqlExpressions.SqlJoinType.Left, otherEntity => x.ItemId == otherEntity.ItemId) })
            ;



            string expectedResult = @"
select	NavItem_2.ItemId as ItemId, NavItem_2.UnitPrice as UnitPrice, a_1.EquipId as EquipId
	from	Equipment as a_1
		left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	where	(NavItem_2.UnitPrice > 500)";
            Test("Relation Navigation Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void Navigation_property_joining_custom_DTO_rather_normal_table()
        {
            var inventoryTransactions = new Queryable<ItemInventoryTransaction>(new QueryProvider());
            var q = inventoryTransactions.Select(x => new { x.TransactionId, x.ItemId, x.NavSummaryLine().TotalCapturedQty, x.NavSummaryLine().TotalQtyGained, x.NavSummaryLine().TotalQtyLost });
            string expectedResult = @"
select a_1.TransactionId as TransactionId, a_1.ItemId as ItemId, NavSummaryLine_2.TotalCapturedQty as TotalCapturedQty, NavSummaryLine_2.TotalQtyGained as TotalQtyGained, NavSummaryLine_2.TotalQtyLost as TotalQtyLost
	from ItemInventoryTransaction as a_1
			left join (
				select a_3.TransactionRowId as TransactionRowId, Sum(a_3.CapturedQty) as TotalCapturedQty, Sum(case when (a_3.NewQty > a_3.CapturedQty) then (a_3.NewQty - a_3.CapturedQty) else 0 end) as TotalQtyGained, Sum(case when (a_3.CapturedQty > a_3.NewQty) then (a_3.CapturedQty - a_3.NewQty) else 0 end) as TotalQtyLost
				from ItemInventoryTransactionDetail as a_3
				group by a_3.TransactionRowId
			) as NavSummaryLine_2 on (a_1.RowId = NavSummaryLine_2.TransactionRowId)
";

            Test("Relation Navigation To Custom Type Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void NavigationLink_attribute_test()
        {
            var inventoryTransactionLines = new Queryable<ItemInventoryTransactionDetail>(new QueryProvider());
            var q = inventoryTransactionLines.Select(x => new { x.NavParentTransaction().TransactionId, x.NavParentTransaction().ItemId, x.CapturedQty, x.NewQty, x.LineStatus });
            string expectedResult = @"
select	NavParentTransaction_2.TransactionId as TransactionId, NavParentTransaction_2.ItemId as ItemId, a_1.CapturedQty as CapturedQty, a_1.NewQty as NewQty, a_1.LineStatus as LineStatus
	from	ItemInventoryTransactionDetail as a_1
		inner join ItemInventoryTransaction as NavParentTransaction_2 on (NavParentTransaction_2.RowId = a_1.TransactionRowId)
";

            Test("Relation Attribute Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Selecting_whole_entity_wrapping_under_sub_query_then_using_navigation_property_over_sub_query()
        {
            Expression<Func<object>> temp = () =>
            queryProvider.DataSet<Equipment>()
            .Take(50)
            .Select(x => new { x.NavItem().ItemId })
            ;

            string expectedResult = @"
select	NavItem_3.ItemId as ItemId
	from	(
		select	top (50)	a_1.EquipId as EquipId, a_1.Model as Model, a_1.ItemId as ItemId
		from	Equipment as a_1
	) as a_2
		left join ItemExtension as NavItem_3 on (NavItem_3.ItemId = a_2.ItemId)
";
            Test("Relation Over Subquery Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void Selecting_navigation_property_in_projection()
        {
            Expression<Func<object>> temp = () =>
            queryProvider.DataSet<Equipment>()
            .Select(x => x.NavItem())
            ;

            string expectedResult = @"
select	NavItem_2.ItemId as ItemId, NavItem_2.UnitPrice as UnitPrice
	from	Equipment as a_1
		left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
";
            Test("Relation Full Selection Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void Selecting_navigation_property_then_selecting_single_column_from_wrapped_sub_query()
        {
            Expression<Func<object>> temp = () =>
            queryProvider.DataSet<Equipment>()
            .Select(x => x.NavItem())
            .Select(x => x.UnitPrice)
            ;

            string expectedResult = @"
select	a_3.UnitPrice as Col1
	from	(
		select	NavItem_2.ItemId as ItemId, NavItem_2.UnitPrice as UnitPrice
		from	Equipment as a_1
			left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	) as a_3
";
            Test("Relation Full Selection Then Sub Query Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void Navigation_property_defined_as_left_outer_join_should_force_subsequent_inner_join_navigations_to_form_left_outer_join()
        {
            // in this test we are seeing that NavItem in Equipment entity is parent optional
            // so the join will be left join, then we see that from NavItem to NavItemBase is
            // 1 to 1 relation but parent NOT optional, which should create inner join but
            // since the first join is left, so later joins will be left join as well

            var equipmentList = new Queryable<Equipment>(this.queryProvider);
            var q = equipmentList
                        .Where(x => x.NavItem().UnitPrice > 500)
                        .Where(x => x.NavItem().NavItemBase().NavItemMoreInfo().TrackingType == "SRN")
                        .Select(x => new
                        {
                            x.NavItem().NavItemBase().NavItemMoreInfo().TrackingType,
                            x.NavItem().NavItemBase().NavItemMoreInfo().ItemId,
                            x.NavItem().NavItemBase().ItemDescription
                        })
            ;

            string expectedResult = @"
select	NavItemMoreInfo_4.TrackingType as TrackingType, NavItemMoreInfo_4.ItemId as ItemId, NavItemBase_3.ItemDescription as ItemDescription
    	from	Equipment as a_1
    		left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
    		left join ItemBase as NavItemBase_3 on (NavItemBase_3.ItemId = NavItem_2.ItemId)
    		left join ItemMoreInfo as NavItemMoreInfo_4 on (NavItemBase_3.ItemId = NavItemMoreInfo_4.ItemId)
    	where	(NavItem_2.UnitPrice > 500) and (NavItemMoreInfo_4.TrackingType = 'SRN')
";
            Test("Complex Navigation Relation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Navigation_properties_used_along_with_join_extension_methods()
        {
            Expression<Func<object>> temp = () =>
            queryProvider.DataSet<Component>()
            .LeftJoin(queryProvider.DataSet<ItemBase>(), (c, i) => new { c, i }, j => j.c.ItemId == j.i.ItemId)
            .LeftJoin(queryProvider.DataSet<Equipment>(), (ds, e) => new { ds, e }, j => j.ds.c.EquipId == j.e.EquipId)
            .Select(x => new { x.ds.c.CompId, CompItem = x.ds.c.ItemId, x.ds.c.NavItem.UnitPrice, x.ds.c.EquipId, x.e.Model, EquipItemDesc = x.e.NavItem().NavItemBase().ItemDescription })
            ;

            string expectedResult = @"
 select	a_1.CompId as CompId, a_1.ItemId as CompItem, NavItem_4.UnitPrice as UnitPrice, a_1.EquipId as EquipId, a_3.Model as Model, NavItemBase_6.ItemDescription as EquipItemDesc
	from	Component as a_1
		left join ItemBase as a_2 on (a_1.ItemId = a_2.ItemId)
		left join Equipment as a_3 on (a_1.EquipId = a_3.EquipId)
		inner join ItemExtension as NavItem_4 on (NavItem_4.ItemId = a_1.ItemId)
		left join ItemExtension as NavItem_5 on (NavItem_5.ItemId = a_3.ItemId)
		left join ItemBase as NavItemBase_6 on (NavItemBase_6.ItemId = NavItem_5.ItemId)
";
            Test("Navigation Plus Join Test", temp.Body, expectedResult);
        }


        [TestMethod]
        public void ToMultipleChildren_navigation_property_test()
        {
            IQueryable<StudentGrade> studentGrades = new Queryable<StudentGrade>(new QueryProvider());
            var q = studentGrades.Where(x => x.NavStudentGradeDetails.Any(y => y.MarksGained > 50));
            string expectedResult = @"
    select a_1.RowId as RowId, a_1.StudentId as StudentId, a_1.Grade as Grade
	from StudentGrade as a_1
	where exists(
			select 1 as Col1
			from StudentGradeDetail as a_2
			where (a_1.RowId = a_2.StudentGradeRowId)
				 and (a_2.MarksGained > 50)
		)
";
            Test("To Multiple Children Navigation Property Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void ToSingleChild_then_ToMultipleChildren_navigation_test()
        {
            IQueryable<ItemBase> items = new Queryable<ItemBase>(new QueryProvider());
            var q = items.Where(x => x.NavItemExt().NavParts.Any(y => y.PartNumber == "123"));
            string expectedResult = $@"
	select	a_1.ItemId as ItemId, a_1.ItemDescription as ItemDescription
	from	ItemBase as a_1
		left join ItemExtension as NavItemExt_2 on (a_1.ItemId = NavItemExt_2.ItemId)
	where	exists(
		select	1 as Col1
		from	ItemPart as a_3
		where	(NavItemExt_2.ItemId = a_3.ItemId) and (a_3.PartNumber = '123')
	)
";
            Test("To Multiple Children With To One Navigation Property Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Navigation_children_property_selected_in_projection_with_Count()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.Select(x => new
            {
                x.EmployeeId,
                x.Name,
                DegreeCount = x.NavDegrees.Count()
            });
            string expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, (
		select	Count(1) as Col1
		from	EmployeeDegree as a_2
		where	(a_1.EmployeeId = a_2.EmployeeId)
	) as DegreeCount
	from	Employee as a_1
";
            Test("Nav Children Sub Query Simple Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Navigation_children_selected_with_GroupBy_Select_Count()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.Select(x => new
            {
                x.EmployeeId,
                x.Name,
                DegreeCount = x.NavDegrees.GroupBy(y => y.EmployeeId).Select(y => new { E_ID = y.Key, MaxDeg = y.Max(z => z.Degree) }).Count()
            });
            string expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, (
		select	Count(1) as Col1
		from	(
			select	a_2.EmployeeId as E_ID, Max(a_2.Degree) as MaxDeg
			from	EmployeeDegree as a_2
			where	(a_1.EmployeeId = a_2.EmployeeId)
			group by a_2.EmployeeId
		) as a_3
	) as DegreeCount
	from	Employee as a_1
";
            Test("Nav Children Sub Query Complex Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Navigation_children_selected_with_SelectMany_then_sub_navigation_children_selected_and_then_Count()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.Select(x => new
            {
                x.EmployeeId,
                x.Name,
                DegreeCount = x.NavDegrees.SelectMany(y => y.NavMarksheets).Count()
            });
            string expectedResult = @"
   	select a_1.EmployeeId as EmployeeId, a_1.Name as Name, (
			select Count(1) as Col1
			from EmployeeDegree as a_2
					inner join Marksheet as NavMarksheets_3 on (a_2.RowId = NavMarksheets_3.EmployeeDegreeRowId)
			where (a_1.EmployeeId = a_2.EmployeeId)
		) as DegreeCount
	from Employee as a_1
";
            Test("Nav Children Sub Query Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Navigation_children_in_Where_with_nested_navigation_children_exists()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                .Where(x => x.NavDegrees.Where(y => y.NavMarksheets.Any(z => z.TotalMarks > 50)).Any())
                .Select(x => new
                {
                    x.EmployeeId,
                    x.Name,
                });
            string expectedResult = @"
    select a_1.EmployeeId as EmployeeId, a_1.Name as Name
	from Employee as a_1
	where exists(
			select 1 as Col1
			from EmployeeDegree as a_2
			where (a_1.EmployeeId = a_2.EmployeeId)
				 and exists(
					select 1 as Col1
					from Marksheet as a_3
					where (a_2.RowId = a_3.EmployeeDegreeRowId)
						 and (a_3.TotalMarks > 50)
				)
		)
";
            Test("Nav Children Sub Query Where Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Navigation_property_that_translates_to_outer_apply_with_1_line_in_return()
        {
            var queryProvider = new QueryProvider();
            var invoice = new Queryable<Invoice>(queryProvider);

            var q = invoice.Select(
                        x => new {
                            x.InvoiceId,
                            Item = x.NavFirstLine().ItemId,
                            x.NavFirstLine().NavItem().ItemDescription,
                            x.NavFirstLine().UnitPrice
                        });

            string expectedResult = @"
    	select a_1.InvoiceId as InvoiceId, NavFirstLine_2.ItemId as Item, NavItem_4.ItemDescription as ItemDescription, NavFirstLine_2.UnitPrice as UnitPrice
	from Invoice as a_1
			outer apply (
				select top (1) a_3.RowId as RowId, a_3.InvoiceId as InvoiceId, a_3.ItemId as ItemId, a_3.UnitPrice as UnitPrice, a_3.Quantity as Quantity, a_3.LineTotal as LineTotal
				from InvoiceDetail as a_3
				where (a_1.RowId = a_3.InvoiceId)
			) as NavFirstLine_2
			left join ItemBase as NavItem_4 on (NavItem_4.ItemId = NavFirstLine_2.ItemId)
";

            Test("Calculated Property to Outer Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Navigation_property_that_translates_to_outer_apply_with_multiple_lines_in_return()
        {
            var queryProvider = new QueryProvider();
            var invoices = new Queryable<Invoice>(queryProvider);
            var invoiceDetails = new Queryable<InvoiceDetail>(queryProvider);
            var q = invoices.Select(x => new { x.InvoiceId, x.NavTop2Lines().ItemId, x.NavTop2Lines().UnitPrice });
            string expectedResult = @"
   	select a_1.InvoiceId as InvoiceId, NavTop2Lines_2.ItemId as ItemId, NavTop2Lines_2.UnitPrice as UnitPrice
	from Invoice as a_1
			outer apply (
				select top (2) a_3.RowId as RowId, a_3.InvoiceId as InvoiceId, a_3.ItemId as ItemId, a_3.UnitPrice as UnitPrice, a_3.Quantity as Quantity, a_3.LineTotal as LineTotal
				from InvoiceDetail as a_3
				where (a_1.RowId = a_3.InvoiceId)
			) as NavTop2Lines_2
";
            Test("Navigation to Child with More than 1 Lines Outer Apply Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Navigation_is_not_null()
        {
            var employee = new Queryable<Employee>(this.queryProvider);
            var q = employee.Where(x => x.NavManager() != null);
            string expectedResult = @"
select a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from Employee as a_1
			inner join Employee as NavManager_2 on (NavManager_2.EmployeeId = a_1.ManagerId)
	where (NavManager_2.RowId is not null)
";
            Test("Navigation Is Not Null Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Navigation_outer_apply_not_null()
        {
            var invoices = new Queryable<Invoice>(this.queryProvider);
            var q = invoices.Where(x => x.NavFirstLine() != null);
            string expectedResult = @"
select a_1.RowId as RowId, a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate, a_1.Description as Description, a_1.CustomerId as CustomerId, a_1.DueDate as DueDate
	from Invoice as a_1
			outer apply (
				select top (1) a_3.RowId as RowId, a_3.InvoiceId as InvoiceId, a_3.ItemId as ItemId, a_3.UnitPrice as UnitPrice, a_3.Quantity as Quantity, a_3.LineTotal as LineTotal
				from InvoiceDetail as a_3
				where (a_1.RowId = a_3.InvoiceId)
			) as NavFirstLine_2
	where (NavFirstLine_2.RowId is not null)
";
            Test("Navigation Outer Apply Is Not Null Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Navigation_in_condition_checking_for_null()
        {
            var invoices = new Queryable<Invoice>(this.queryProvider);
            var q = invoices.Select(x => new { InvoiceFirstLineRowId = x.NavFirstLine() != null ? (Guid?)x.NavFirstLine().RowId : null });
            string expectedResult = @"
select NavFirstLine_2.RowId as InvoiceFirstLineRowId
	from Invoice as a_1
			outer apply (
				select top (1) a_3.RowId as RowId, a_3.InvoiceId as InvoiceId, a_3.ItemId as ItemId, a_3.UnitPrice as UnitPrice, a_3.Quantity as Quantity, a_3.LineTotal as LineTotal
				from InvoiceDetail as a_3
				where (a_1.RowId = a_3.InvoiceId)
			) as NavFirstLine_2
";
            Test("Navigation in Condition Check for Null Test", q.Expression, expectedResult);
        }
    }
}
