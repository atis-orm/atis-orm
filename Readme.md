
# Atis ORM

> **⚠️ Work in Progress**  
> This project is currently under development and **has not been published as a NuGet package yet**.  
> The structure, APIs, and internal logic are subject to change without notice.  
> Please use it **for review and experimental purposes only**.

---

## Overview

**Atis ORM** is a lightweight and extensible library focused on **transforming LINQ expression trees into structured SQL expression representations (ASTs)**.  

It is designed to serve as the core query engine of a future ORM system, with a strong emphasis on **customization, plugin-based normalization, and minimalism**.

The current version focuses purely on the **transformation and modeling layer**, enabling advanced SQL query generation and inspection.   Query execution and materialization will be introduced in future phases.

---

## Objectives

- **Enable Complex SQL via LINQ** — The primary goal of Atis ORM is to empower developers to write **nearly any type of complex SQL** `SELECT` query using LINQ syntax, including deeply nested queries, recursive CTEs, outer applies, and dynamic projections — all translated into a structured SQL AST.
- **Expression-Centric Design** — Focused on transforming LINQ expression trees into structured, strongly-typed SQL expression models (ASTs), rather than generating SQL strings directly.
- **Plugin-Based Normalization** — Provides a modular and plugin-friendly architecture, where expression normalization, conversion, and post-processing can be extended or replaced without modifying the core.
- **Support for Complex Query Scenarios**, including:
  - Recursive CTEs
  - Navigation chains with smart join inference
  - Complex navigations (Outer Apply, etc.)
  - Calculated properties
  - Specification pattern integration
  - Bulk update/delete feature
  - etc.
- **Minimalism & Explicit Control** — No built-in model discovery, conventions, or tracking. The system avoids magic — every behavior is explicitly controlled and injectable.
- ** Ideal for Complex Business Applications** — Targets enterprise scenarios where centralizing business rules and dynamic query shaping are essential.

---

## Examples

### Direct Select without From

**LINQ**

```csharp
var q = dbc.Select(() => new { n = 1 })
                .Where(x => x.n > 5);
```

**SQL**

```sql
select	a_1.n as n
from	(
    select	1 as n
) as a_1
where	(n > 5)
```

---

### Explicit Joins

**LINQ**
```csharp
var q = employees
        .OuterApply(e => employeeDegrees.Where(d => d.EmployeeId == e.EmployeeId).Take(1), (e, ed) => new { e, ed })
        .Select(x => new { x.e.EmployeeId, x.e.Name, x.ed.Degree });
```

**SQL**
```sql
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree
from	Employee as a_1
	outer apply (
		select	top (1)	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, 
                a_2.Degree as Degree, a_2.University as University
		from	EmployeeDegree as a_2
		where	(a_2.EmployeeId = a_1.EmployeeId)
	) as a_3
```

---

### Recursive Query

**LINQ**
```csharp
var q = employees
            .Where(x => x.ManagerId == null)
            .RecursiveUnion(anchor => anchor.SelectMany(anchorMember => anchorMember.NavSubOrdinates))
            .Select(x => new { x.EmployeeId, x.Name, x.ManagerId });
```

**SQL**
```sql
with cte_1 as 
(	
	select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, 
            a_2.Department as Department, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	NavSubOrdinates_4.RowId as RowId, NavSubOrdinates_4.EmployeeId as EmployeeId, 
            NavSubOrdinates_4.Name as Name, NavSubOrdinates_4.Department as Department, 
            NavSubOrdinates_4.ManagerId as ManagerId	
	from	cte_1 as a_3	
		    inner join Employee as NavSubOrdinates_4 on (a_3.EmployeeId = NavSubOrdinates_4.ManagerId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
```

---

### Recursive Query as Sub-Query

**LINQ**
```csharp
var q = from manager in employees
        where manager.EmployeeId == "123"       // picking specific manager
        select new
        {
            ManagerId = manager.EmployeeId,
            ManagerName = manager.Name,
            NestedCount = employees
                            .Where(immediateChild => immediateChild.ManagerId == manager.EmployeeId)
                            .RecursiveUnion(anchor => anchor.SelectMany(anchorMember => anchorMember.NavSubOrdinates))
                            .Count()
        };
```

**SQL**
```sql
with cte_1 as 
(	
    -- we picked up the query that was materialized upto the point when RecursiveUnion found
    -- and moved into cte_1
	select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, 
            a_2.Name as Name, a_2.Department as Department, 
            a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	(a_2.EmployeeId = '123')	
), cte_3 as 
(	
	select	a_4.RowId as RowId, a_4.EmployeeId as EmployeeId, 
            a_4.Name as Name, a_4.Department as Department, 
            a_4.ManagerId as ManagerId	
	from	Employee as a_4	
            cross join cte_1 as cte_1	            -- auto resolved reference for manager.EmployeeId
	where	(a_4.ManagerId = cte_1.EmployeeId)	    -- cte_1.EmployeeId is manager.EmployeeId
	union all	
	select	NavSubOrdinates_6.RowId as RowId, NavSubOrdinates_6.EmployeeId as EmployeeId, 
            NavSubOrdinates_6.Name as Name, NavSubOrdinates_6.Department as Department, 
            NavSubOrdinates_6.ManagerId as ManagerId	
	from	cte_3 as a_5	
		    inner join Employee as NavSubOrdinates_6 on (a_5.EmployeeId = NavSubOrdinates_6.ManagerId)	
)
select	cte_1.EmployeeId as ManagerId, cte_1.Name as ManagerName, 
        (select	Count(1) as Col1 from cte_3 as cte_3) as NestedCount
from	cte_1 as cte_1
```

---

### Navigation Properties

**LINQ**
```csharp
var q = equipmentList
            // Equipment.NavItem : Equipment (child) -> Item (parent) is nullable which will render as left join
            .Where(x => x.NavItem().UnitPrice > 500)
            // Item (child) -> ItemBase (parent) is not-nullable which should render as inner join
            // however, because of previous left join this join should render as left as well
            // similarly, all later joins should become left even if they are inner
            .Where(x => x.NavItem().NavItemBase().NavItemMoreInfo().TrackingType == "SRN")
            .Select(x => new
            {
                x.NavItem().NavItemBase().NavItemMoreInfo().TrackingType,
                x.NavItem().NavItemBase().NavItemMoreInfo().ItemId,
                x.NavItem().NavItemBase().ItemDescription
            });
```

**SQL**
```sql
select	NavItemMoreInfo_4.TrackingType as TrackingType, 
        NavItemMoreInfo_4.ItemId as ItemId, 
        NavItemBase_3.ItemDescription as ItemDescription
from	Equipment as a_1
		left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
		left join ItemBase as NavItemBase_3 on (NavItemBase_3.ItemId = NavItem_2.ItemId)
		left join ItemMoreInfo as NavItemMoreInfo_4 on (NavItemBase_3.ItemId = NavItemMoreInfo_4.ItemId)
where	(NavItem_2.UnitPrice > 500) and 
        (NavItemMoreInfo_4.TrackingType = 'SRN')
```

---

### Navigation Property Encapsulating Recursive Query

**LINQ**
```csharp
var q = from manager in employees
        where manager.EmployeeId == "123"       // picking specific manager
        select new
        {
            ManagerId = manager.EmployeeId,
            ManagerName = manager.Name,
            // NavNestedChildren internally is using RecursiveUnion
            FilteredNested = manager.NavNestedChildren.Where(x => x.NavEmployee().Department == "IT").Count(),
        };
```

**SQL**
```sql
with cte_1 as 
(	
	select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, 
            a_2.Name as Name, a_2.Department as Department,
            a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	(a_2.EmployeeId = '123')	
), cte_3 as 
(	
	select	a_4.RowId as RowId, a_4.EmployeeId as EmployeeId, 
            a_4.Name as Name, a_4.Department as Department, 
            a_4.ManagerId as ManagerId	
	from	Employee as a_4	
		    cross join cte_1 as cte_1	
	where	(cte_1.EmployeeId = a_4.ManagerId)	
	union all	
	select	NavSubOrdinates_6.RowId as RowId, NavSubOrdinates_6.EmployeeId as EmployeeId, 
            NavSubOrdinates_6.Name as Name, NavSubOrdinates_6.Department as Department, 
            NavSubOrdinates_6.ManagerId as ManagerId	
	from	cte_3 as a_5	
		    inner join Employee as NavSubOrdinates_6 on (a_5.EmployeeId = NavSubOrdinates_6.ManagerId)	
)
select	cte_1.EmployeeId as ManagerId, cte_1.Name as ManagerName, 
        (
	    select	Count(1) as Col1
	    from	(
		        select	cte_3.EmployeeId as EmployeeId, cte_3.ManagerId as ImmediateManagerId, cte_1.EmployeeId as TopManagerId
		        from	cte_3 as cte_3
	            ) as a_7
		        inner join Employee as NavEmployee_8 on (NavEmployee_8.EmployeeId = a_7.EmployeeId)
        where	(a_7.TopManagerId = a_7.TopManagerId) and 
                (NavEmployee_8.Department = 'IT')
        ) as FilteredNested
from	cte_1 as cte_1
```

---

### Calculated Properties

```csharp
// Calculated property that can be used within LINQ query as well as
// in-memory data manipulation after loaded in C#
[CalculatedProperty(nameof(CalcPercentageExpression))]
public decimal? CalcPercentage => CalcPercentageCompiled(this);

// This will be used by converter to translate into SQL
public static readonly Expression<Func<Marksheet, decimal?>> CalcPercentageExpression = 
    m => m.TotalMarks > 0 ? m.MarksGained / m.TotalMarks * 100.0m : 0;

// this is to centralize the logic so that we don't have to 
// write the same logic 2 times, i.e., one for translation and one to be used
// for in-memory
public static readonly Func<Marksheet, decimal?> CalcPercentageCompiled = CalcPercentageExpression.Compile();
```

**LINQ**
```csharp
var q = marksheets.Where(x => x.CalcPercentage > 50).Select(x => new { x.Course, x.Grade });
```

**SQL**
```sql
select	a_1.Course as Course, a_1.Grade as Grade
from	Marksheet as a_1
where	(case when (a_1.TotalMarks > 0) then ((a_1.MarksGained / a_1.TotalMarks) * 100.0) else 0 end > 50)
```

---

### Specification Pattern

```csharp
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
```

**LINQ**
```csharp
// here x.InvoiceDate is being supplied as parameter which will 
// replace the property in the expression
var q = invoices.Where(x => new InvoiceIsDueOnGivenDateSpecification(x.InvoiceDate).IsSatisfiedBy(x))
                  .Where(x => !new CustomerIsInvalidSpecification().IsSatisfiedBy(x.NavCustomer()));
```

**SQL**
```sql
select	a_1.RowId as RowId, a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate, 
        a_1.Description as Description, a_1.CustomerId as CustomerId, a_1.DueDate as DueDate
from	Invoice as a_1
		inner join Customer as NavCustomer_2 on (NavCustomer_2.RowId = a_1.CustomerId)
where	(a_1.DueDate >= a_1.InvoiceDate) and 
        not ((NavCustomer_2.Status = 'Disabled') or (NavCustomer_2.Status = 'Blocked'))
```

---

### Complex Outer Apply Navigation

```csharp
public class InvoiceWithInvoiceDetailFirstLineRelation : EntityRelation<Invoice, InvoiceDetail>
{
    // JoinExpression = null will make it outer apply
    public override Expression<Func<Invoice, InvoiceDetail, bool>>? JoinExpression => null;
    public override Expression<Func<Invoice, IQueryable<InvoiceDetail>>>? FromParentToChild(IQueryProvider queryProvider)
    {
        return parent => parent.NavLines.Top(1);
    }
}
```

**LINQ**
```csharp
var q = invoice.Select(
            x => new { 
                    x.InvoiceId, 
                    Item = x.NavFirstLine().ItemId, 
                    x.NavFirstLine().NavItem().ItemDescription, 
                    x.NavFirstLine().UnitPrice 
            });
```

**SQL**
```sql
select	a_1.InvoiceId as InvoiceId, NavFirstLine_3.ItemId as Item, 
        NavItem_4.ItemDescription as ItemDescription, NavFirstLine_3.UnitPrice as UnitPrice
from	Invoice as a_1
        outer apply (
            select	top (1)	a_2.RowId as RowId, a_2.InvoiceId as InvoiceId, a_2.ItemId as ItemId, 
                    a_2.UnitPrice as UnitPrice, a_2.Quantity as Quantity, a_2.LineTotal as LineTotal
            from	InvoiceDetail as a_2
            where	(a_1.RowId = a_2.InvoiceId)
        ) as NavFirstLine_3
        left join ItemBase as NavItem_4 on (NavItem_4.ItemId = NavFirstLine_3.ItemId)
```

---

### Bulk Update

**LINQ**
```csharp
Expression<Func<int>> expr = () => (
                                    from asset in assets
                                    join item in items on asset.ItemId equals item.ItemId
                                    select new { asset, item }                  // joined 2 tables in 1 query
                                   )
                                   .Update(                                     // <- Update query
                                        ms => ms.item,                          // <- which table to update
                                        ms => new ItemBase                      // <- which fields to update
                                            { 
                                                ItemDescription = ms.item.ItemDescription + ms.asset.SerialNumber 
                                            }, 
                                        ms => ms.asset.SerialNumber == "123"    // <- where condition
                                    );
```

**SQL**
```sql
update a_1
    set ItemDescription = (a_1.ItemDescription + a_2.SerialNumber)
from	Asset as a_2
	    inner join ItemBase as a_1 on (a_2.ItemId = a_1.ItemId)
where	(a_2.SerialNumber = '123')
```

---

## Contribution

⚙️ **Work in Progress** — Contributions are currently not open until the first stable draft is completed.  
However, feel free to explore and suggest improvements via issues.

---
