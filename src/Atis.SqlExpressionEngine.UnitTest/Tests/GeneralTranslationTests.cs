﻿// Bismilla-hir-Rahman-nir-Rahim
// In the name of Allah the most beneficial and merciful.
/*
 * Rules:
 *  1) We don't allow the translation system to perform nested translation within the converter. For example,
 *  ﻿   in Binary Expression converter plugin we will receive left and right node already converted, Binary Expression
 *  ﻿   will NOT be accessing main converter system to do any further conversion.
 *  ﻿   Another scenario is that, let say we have a variable in the expression of type of IQueryable<T>, like
 *  ﻿       var q1 = QueryExtensions.DataSet<Student>().Where(x => x.IsDisabled == false);
 *  ﻿       var q2 = QueryExtensions.DataSet<StudentGrade>().Where(x => q1.Any(y => y.StudentId == x.StudentId));
 *  ﻿   'q1' is going to be received by ConstantExpressionConverter, and constant expression converter
 *  ﻿   simply creates a parameter expression. But in this case, we might think that why not create a separate converter
 *     which will go in Expression property of q1 and can perform further conversion. But as this rule states
 *  ﻿   that this should NOT be done, because converter plugin has no access to translation system.
 *  ﻿   
 */

using System.Linq.Expressions;
using Atis.SqlExpressionEngine.UnitTest.Metadata;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class GeneralTranslationTests : TestBase
    {

        [TestMethod]
        public void GeneralTest()
        {
            //var hashGenerator = new ExpressionHashGenerator(new QueryComponentIdentifier(), new ReflectionService());

            //IQueryable<StudentGrade> q1 = new Queryable<StudentGrade>(new QueryProvider());

            //IQueryable<Student> sq1 = new Queryable<Student>(new QueryProvider())
            //            .Where(x => q1.Any(y => y.StudentId == x.StudentId));
            //var h1 = hashGenerator.GenerateHash(sq1.Expression);

            //var grade = "A";
            //q1 = q1.Where(x => x.Grade == grade);

            //IQueryable<Student> sq2 = new Queryable<Student>(new QueryProvider())
            //            .Where(x => q1.Any(y => y.StudentId == x.StudentId));
            //var h2 = hashGenerator.GenerateHash(sq2.Expression);

            //grade = null;
            //IQueryable<Student> sq3 = new Queryable<Student>(new QueryProvider())
            //            .Where(x => q1.Any(y => y.StudentId == x.StudentId));
            //var h3 = hashGenerator.GenerateHash(sq2.Expression);

            //Console.WriteLine(h1);
            //Console.WriteLine(h2);
            //Console.WriteLine(h3);

            //var employees = new Queryable<Employee>(this.dbc);
            //var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);

            //var q2 = employees.SelectMany(e => QueryExtensions.ChildJoin(e, employeeDegrees.Where(x => x.Degree == "123"), x => x.EmployeeId == e.EmployeeId && x.RowId == e.RowId, NavigationType.ToChildren, "EmployeeDegrees"));

            //var q = employees.SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => (x.Degree == "123" || x.Degree == "665") && x.RowId == e.RowId));
            //var preprocessedExpression = PreprocessExpression(q.Expression);
            //Console.WriteLine(preprocessedExpression);
            //var selectManyJoinConverter = new ChildJoinReplacementVisitor();
            //var updatedExpression = selectManyJoinConverter.Visit(preprocessedExpression);
            //Console.WriteLine(updatedExpression);

            //var employees = new Queryable<Employee>(this.dbc);
            //var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            //var q = from e in employees
            //        from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId)
            //        from m in employees.Where(x => x.EmployeeId == e.ManagerId)
            //        select new { e.Name, ed.Degree, ManagerName = m.Name };

            //var testVisitor = new TestVisitor();
            //testVisitor.Visit(q.Expression);

            //var query = new Queryable<Asset>(new QueryProvider());
            //if (query is IQueryable<IModelWithItem> queryWithItem)
            //{
            //    var q = queryWithItem.Where(x => x.NavItem().ItemDescription.Contains("abc")).Select(x => x).Where(x => x.NavItem().ItemId == "123");
            //    var preprocessedExpression = PreprocessExpression(q.Expression);
            //    Console.WriteLine(q.Expression);
            //    Console.WriteLine(preprocessedExpression);
            //}
        }

        [TestMethod]
        public void DataContext_basic_test()
        {
            var q = this.dataContext.Invoices.Select(x => new { x.InvoiceId, x.InvoiceDate });
            string expectedResult = @"
select	a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate
	from	Invoice as a_1
";
            Test("DataContext basic test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Single_table_without_any_method()
        {
            var q = new Queryable<Student>(queryProvider);
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1";
            Test("Simple Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Where_method_call()
        {
            var q = new Queryable<Student>(queryProvider).Where(x => x.StudentId == "123");
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.StudentId = '123')";
            Test("Where Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Select_method_call()
        {
            var q = new Queryable<Student>(queryProvider).Select(x => new { x.StudentId, x.Name });
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
from	Student as a_1";
            Test("Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Where_Select_OrderBy_Take_method_calls()
        {
            var q = new Queryable<Student>(queryProvider)
                .Where(x => x.StudentId == "55")
                .Where(x => x.Name.Contains("Jhon"))
                .Select(x => new
                {
                    Id = x.StudentId,
                    x.Address,
                    AddressLength = x.Address.Length,
                    Age = x.Age.Value
                })
                .OrderBy(x => x.Id)
                .Take(5)
                ;
            var expectedResult = @"
select	top (5)	a_1.StudentId as Id, a_1.Address as Address, CharLength(a_1.Address) as AddressLength, a_1.Age as Age
from	Student as a_1
where	(a_1.StudentId = '55')
	    and	(a_1.Name like '%' + 'Jhon' + '%')
order by Id asc";
            Test("Full Simple Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Where_after_Select_should_wrap_in_sub_query()
        {
            var q = new Queryable<Student>(queryProvider)
                .Select(x => new { Id = x.StudentId })
                .Where(x => x.Id == "123");
            var expectedResult = @"
select	a_2.Id as Id
from	(
	select	a_1.StudentId as Id
	from	Student as a_1
) as a_2
where	(a_2.Id = '123')";
            Test("Simple Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Take_after_Where_then_Where_and_Take_should_wrap_in_sub_query()
        {
            var q = new Queryable<Student>(queryProvider)
                .Where(x => x.StudentId == "2")
                .Take(5)
                .Where(x => x.Address.ToLower().Contains("US"))
                .Take(2)
                ;
            var expectedResult = @"
select	top (2)	a_2.StudentId as StudentId, a_2.Name as Name, a_2.Address as Address, a_2.Age as Age, a_2.AdmissionDate as AdmissionDate, a_2.RecordCreateDate as RecordCreateDate, a_2.RecordUpdateDate as RecordUpdateDate, a_2.StudentType as StudentType, a_2.CountryID as CountryID, a_2.HasScholarship as HasScholarship
	from	(
		select	top (5)	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
		from	Student as a_1
		where	(a_1.StudentId = '2')
	) as a_2
	where	(ToLower(a_2.Address) like '%' + 'US' + '%')";
            Test("Complex Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Sub_query_in_Select_and_in_Where()
        {
            var q = new Queryable<Student>(queryProvider)
                .Select(x => new
                {
                    Id = x.StudentId,
                    Grd = queryProvider.DataSet<StudentGrade>().Where(y => y.StudentId == x.StudentId).Select(y => y.Grade).FirstOrDefault()
                })
                .Where(x => queryProvider.DataSet<StudentGrade>().Where(y => y.StudentId == x.Id).Any());
            var expectedResult = @"
select	a_3.Id as Id, a_3.Grd as Grd
from	(
	select	a_1.StudentId as Id, (
		select	top (1)	a_2.Grade as Col1
		from	StudentGrade as a_2
		where	(a_2.StudentId = a_1.StudentId)
	) as Grd
	from	Student as a_1
) as a_3
where	exists(
	select	1 as Col1
	from	StudentGrade as a_4
	where	(a_4.StudentId = a_3.Id)
)";
            Test("Other Table Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void FirstOrDefault_with_predicate_having_a_sub_query_with_Select_and_then_again_FirstOrDefault_on_sub_query_with_predicate()
        {
            Expression<Func<object>> queryExpression = () => queryProvider.DataSet<Student>()
                .Select(x => new { Id = x.StudentId })
                .FirstOrDefault(
                        x => x.Id == "123" &&
                        queryProvider.DataSet<StudentGrade>()
                        .Select(y => y.Grade)
                        .FirstOrDefault(y2 => y2 == "A") == "20"
                );
            var expecectedResult = @"
select  top (1) a_2.Id as Id
from    (
        select  a_1.StudentId as Id
        from    Student as a_1
) as a_2
where ((a_2.Id = '123')
        and ((
                select  top (1) a_4.Col1 as Col1
                from    (
                        select  a_3.Grade as Col1
                        from    StudentGrade as a_3
                ) as a_4
                where   (a_4.Col1 = 'A')
        ) = '20'))";
            Test("Complex Nested FirstOrDefault Test", queryExpression.Body, expecectedResult);
        }

        private class StudentQueryResult
        {
            public string StudentId { get; set; }
            public string StudentName { get; set; }
            public string Grade { get; set; }
        }

        [TestMethod]
        public void MemberInitExpression_in_Select()
        {
            Expression<Func<object>> queryExpression = () =>
            queryProvider.DataSet<Student>()
            .Select(x => new StudentQueryResult
            {
                StudentId = x.StudentId,
                StudentName = x.Name,
                Grade = queryProvider.DataSet<StudentGrade>().Where(y => y.StudentId == x.StudentId).Select(y => y.Grade).FirstOrDefault()
            });
            string expectedResult = @"
select  a_1.StudentId as StudentId, a_1.Name as StudentName, (
                select  top (1) a_2.Grade as Col1
                from    StudentGrade as a_2
                where   (a_2.StudentId = a_1.StudentId)
        ) as Grade
from    Student as a_1
";
            Test("Member Init Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void Query_variable_used_in_another_query()
        {
            IQueryable<Student> queryStudent = new Queryable<Student>(new QueryProvider());
            IQueryable<StudentGrade> queryGrade = new Queryable<StudentGrade>(new QueryProvider()).Where(x => x.Grade == "5");
            queryStudent = queryStudent.Where(x => x.Name.Contains("Abc"));
            queryStudent = queryStudent.Where(x => queryGrade.Any(y => y.StudentId == x.StudentId));

            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Name like '%' + 'Abc' + '%') and exists(
		select	1 as Col1
		from	StudentGrade as a_2
		where	(a_2.Grade = '5') and (a_2.StudentId = a_1.StudentId)
	)
";
            Test("Query Variable Test", queryStudent.Expression, expectedResult);
        }

        
        [TestMethod]
        public void Having_query_extension_method_without_GroupBy()
        {
            var s = new Queryable<Student>(new QueryProvider());
            var studentGrades = new Queryable<StudentGrade>(new QueryProvider());
            var q = s.Where(x => studentGrades.Where(y => y.StudentId == x.StudentId).Having(y => y.Count() > 1).Any());

            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	exists(
		select	1 as Col1
		from	StudentGrade as a_2
		where	(a_2.StudentId = a_1.StudentId)
		having	(Count(1) > 1)
	)";
            Test("Having without Group By Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void UnionAll_query_extension_method_call()
        {
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .UnionAll(s.Where(x => x.Address.Contains("Town")).Select(x => new { x.Name, Id = x.StudentId }))
                        ;
            string expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like '%' + 'City' + '%')
union all
select	a_2.Name as Name, a_2.StudentId as Id
from	Student as a_2
where	(a_2.Address like '%' + 'Town' + '%')";
            Test("UnionAll Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Union_method_call()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => x.Address.Contains("City"))
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .Union(students.Where(x => x.Address.Contains("Town")).Select(x => new { x.Name, Id = x.StudentId }))
                        ;
            string expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like '%' + 'City' + '%')
union
select	a_2.Name as Name, a_2.StudentId as Id
from	Student as a_2
where	(a_2.Address like '%' + 'Town' + '%')
";
            Test("Union Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Coalesce_test()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => x.HasScholarship ?? false).Select(x => new { x.StudentId, x.Name });
            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
	from	Student as a_1
	where	(isnull(a_1.HasScholarship, 0) = 1)";
            Test("Where Boolean Coalesce Condition Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void WhereOr_query_extension_method_call()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => x.Name.StartsWith("555"));
            q = q.Where(x => 1 == 2);
            q = q.WhereOr(x => x.CountryID == "1");
            q = q.WhereOr(x => x.CountryID == "2");
            q = q.Where(x => x.HasScholarship == true);
            string expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, 
        a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, 
        a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
from	Student as a_1
where	(a_1.Name like '555' + '%') and 
        ((0 = 1) or (a_1.CountryID = '1') or (a_1.CountryID = '2')) and (a_1.HasScholarship = 1)
";
            Test("Where OR Condition Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Polymorphism_test()
        {
            var entities = new Queryable<Asset>(new QueryProvider());
            var equipment = new Queryable<Equipment>(new QueryProvider());

            internalMethod1(entities);
            internalMethod2(entities);
            internalMethod3(entities);
            internalMethod4(entities);

            void internalMethod1<T>(IQueryable<T> query) where T : class
            {
                if (query is IQueryable<IModelWithItem>)
                {
                    var q = query.Where(x => ((IModelWithItem)x).NavItem().ItemDescription.Contains("Abc"));
                    string expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
	from	Asset as a_1
		inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	where	(NavItem_2.ItemDescription like '%' + 'Abc' + '%')
";
                    Test("Casting Within Query Test-01", q.Expression, expectedResult);
                }
            }

            void internalMethod2<T>(IQueryable<T> query) where T : class
            {
                if (query is IQueryable<IModelWithItem> queryWithItem)
                {
                    var q = equipment.Where(e => queryWithItem.Where(x => e.ItemId == x.NavItem().ItemId).Where(x => x.NavItem().ItemDescription.Contains("Abc")).Any());
                    // Where(Where(queryWithItem, x => 5 > 1), x => x.NavItem().ItemDescription.Contains("Abc"))
                    string expectedResult = @"
select	a_1.EquipId as EquipId, a_1.Model as Model, a_1.ItemId as ItemId
	from	Equipment as a_1
	where	exists(
		select	1 as Col1
		from	Asset as a_2
			inner join ItemBase as NavItem_3 on (NavItem_3.ItemId = a_2.ItemId)
		where	(a_1.ItemId = NavItem_3.ItemId) and (NavItem_3.ItemDescription like '%' + 'Abc' + '%')
	)
";
                    Test("Casting Within Query Test-02", q.Expression, expectedResult);
                }
            }

            void internalMethod3<T>(IQueryable<T> query) where T : IModelWithItem
            {
                var q = query.Where(x => x.NavItem().ItemDescription.Contains("Abc"));
                string expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
	from	Asset as a_1
		inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	where	(NavItem_2.ItemDescription like '%' + 'Abc' + '%')
";
                Test("Casting Within Query Test-03", q.Expression, expectedResult);
            }

            void internalMethod4<T>(IQueryable<T> query) where T : class
            {
                if (query is IQueryable<IModelWithItem> queryWithItem)
                {
                    var q = queryWithItem.Where(x => x.NavItem().ItemId == "333").Select(x => x).Select(x => new { x.NavItem().ItemId, x.NavItem().ItemDescription });
                    string expectedResult = @"
select	NavItem_4.ItemId as ItemId, NavItem_4.ItemDescription as ItemDescription
	from	(
		select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
		from	Asset as a_1
			inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
		where	(NavItem_2.ItemId = '333')
	) as a_3
		inner join ItemBase as NavItem_4 on (NavItem_4.ItemId = a_3.ItemId)
";
                    Test("Casting Within Query Test-04", q.Expression, expectedResult);
                }
            }
        }

        [TestMethod]
        public void Join_extension_method_after_Where_should_do_the_join_on_sub_query()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            var items = new Queryable<ItemBase>(this.queryProvider);
            var q = assets.Where(x => x.ItemId == "123")
                .LeftJoin(items, (a, i) => new { a, i }, (j) => j.a.ItemId == j.i.ItemId)
                .Select(x=> new { x.a.ItemId, x.a.SerialNumber, x.i.ItemDescription, i2 = x.i.ItemId });
            ;
            string expectedResult = @"
select	a_2.ItemId as ItemId, a_2.SerialNumber as SerialNumber, a_3.ItemDescription as ItemDescription, a_3.ItemId as i2
	from	(
		select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
		from	Asset as a_1
		where	(a_1.ItemId = '123')
	) as a_2
		left join ItemBase as a_3 on (a_2.ItemId = a_3.ItemId)
";
            Test("Join After Where Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Standard_join_after_where_should_do_the_join_on_sub_query()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            var items = new Queryable<ItemBase>(this.queryProvider);
            var q = from asset in assets
                    where asset.ItemId == "123"
                    join item in items on asset.ItemId equals item.ItemId into itemGroup
                    from item in itemGroup.DefaultIfEmpty()
                    select new { asset.ItemId, asset.SerialNumber, item.ItemDescription, i2 = item.ItemId };
            ;
            string expectedResult = @"
select	a_2.ItemId as ItemId, a_2.SerialNumber as SerialNumber, a_3.ItemDescription as ItemDescription, a_3.ItemId as i2
	from	(
		select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
		from	Asset as a_1
		where	(a_1.ItemId = '123')
	) as a_2
		left join ItemBase as a_3 on (a_2.ItemId = a_3.ItemId)
";
            Test("Join After Where Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Distinct_method_call()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            var q = assets.Select(x => new { x.ItemId, x.SerialNumber }).Distinct();
            string expectedResult = @"
select  distinct a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
    from	Asset as a_1
";
            Test($"Distinct Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void Variable_nested_in_properties_should_translate_to_SqlParameterExpression()
        {
            var p = new { p1 = new { p2 = new { v = "555" } } };
            var assets = new Queryable<Asset>(this.queryProvider);
            var q = assets.Where(x => x.ItemId == p.p1.p2.v);
            string expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
	from	Asset as a_1
	where	(a_1.ItemId = '555')
";
            Test("Nested Constant Property Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Where_Select_Then_Where_Test()
        {
            var students = new Queryable<Student>(queryProvider);
            var q = students
                .Where(p => p.Age > 18)
                .Select(p => p.Name)
                .Where(name => name.StartsWith("A"));

            string expectedResult = @"
    select	a_2.Col1 as Col1
	from	(
		select	a_1.Name as Col1
		from	Student as a_1
		where	(a_1.Age > 18)
	) as a_2
	where	(a_2.Col1 like 'A' + '%')
";

            Test("Where followed by Select then another Where should wrap correctly", q.Expression, expectedResult);
        }

        [TestMethod]
        public void All_method_call()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var q = employees.Where(x => x.NavDegrees.All(y => y.University == "MIT"));
            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	not exists(
		select	1 as Col1
		from	EmployeeDegree as a_2
		where	(a_1.EmployeeId = a_2.EmployeeId) and not (a_2.University = 'MIT')
	)
";
            Test("All Method Call Test", q.Expression, expectedResult);
        }

        

        [TestMethod]
        public void Explicit_outer_apply_test()
        {
            var employees = new Queryable<Employee>(this.queryProvider);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.queryProvider);
            var q = employees
                        .OuterApply(e => employeeDegrees.Where(d => d.EmployeeId == e.EmployeeId).Take(1), (e, ed) => new { e, ed })
                        .Select(x => new { x.e.EmployeeId, x.e.Name, x.ed.Degree });

            string expectedResult = @"
    select a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree
	from Employee as a_1
			outer apply (
				select top (1) a_3.RowId as RowId, a_3.EmployeeId as EmployeeId, a_3.Degree as Degree, a_3.University as University
				from EmployeeDegree as a_3
				where (a_3.EmployeeId = a_1.EmployeeId)
			) as a_2
";

            Test("Explicit Outer Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Custom_business_method_in_query_test()
        {
            var pidMaster = new Queryable<PID_Master>(this.queryProvider);
            var q = pidMaster.Where(x => x.PID == "123")
                        .Select(x => new { x.PID, FullName = FullName(x.FNAME, x.LNAME) });
            string expectedResult = @"
select	a_1.PID as PID, ((a_1.FNAME + ' ') + a_1.LNAME) as FullName
from	PID_Master as a_1
where	(a_1.PID = '123')
";
            Test("Custom Business Method Test", q.Expression, expectedResult);
        }

        // SQL query compatible concatenation
        public static readonly Expression<Func<string, string, string>> FullNameExpression = (firstName, lastName) => firstName + " " + lastName;
        
        [PseudoMethod(nameof(FullNameExpression))]
        public static readonly Func<string, string, string> FullName = FullNameExpression.Compile();

        [TestMethod]
        public void Negation_test()
        {
            var invoiceDetails = new Queryable<InvoiceDetail>(queryProvider);
            var q = invoiceDetails.Where(x => x.LineTotal > -1).Select(x => new { C1 = -x.LineTotal * -1 });
            string expectedResult = @"
select	(-a_1.LineTotal * -1) as C1
	from	InvoiceDetail as a_1
	where	(a_1.LineTotal > -1)
";

            Test("Negation Test", q.Expression, expectedResult);
        }

        [PseudoMethod(nameof(IsValidDesignationForSoftwareEngineeringExpression))]
        public static bool IsValidDesignationForSoftwareEngineering(string designation)
        {
            return IsValidDesignationForSoftwareEngineeringDelegate(designation);
        }
        static string[] DesignationsForSoftwareEngineering
        {
            get
            {
                return new string[] { "Developer", "Architect", "Programmer" };
            }
        }

        public static readonly Expression<Func<string, bool>> IsValidDesignationForSoftwareEngineeringExpression = (designation) => DesignationsForSoftwareEngineering.Contains(designation);
        private static readonly Func<string, bool> IsValidDesignationForSoftwareEngineeringDelegate = IsValidDesignationForSoftwareEngineeringExpression.Compile();

        [TestMethod]
        public void Custom_business_method_with_not_in_test()
        {
            var employees = new Queryable<EmployeeExtension>(queryProvider);
            var q = employees.Where(x => !IsValidDesignationForSoftwareEngineering(x.Designation) || x.ManagerId != null);

            string expectedResult = @"
select	a_1.Designation as Designation, a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	EmployeeExtension as a_1
	where	(not a_1.Designation in ('Developer','Architect','Programmer') or (a_1.ManagerId is not null))
";

            Test("Custom Business Method with NOT IN Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Exists_with_literal_boolean_flag_should_remove_boolean_literal()
        {
            // this test is specially designed to be used with Sql Server
            var employees = new Queryable<Employee>(queryProvider);
            var q = employees.Where(x => x.NavDegrees.Any() == false);
            string expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	(case when exists(
		select	1 as Col1
		from	EmployeeDegree as a_2
		where	(a_1.EmployeeId = a_2.EmployeeId)
	) then 1 else 0 end = 0)
";

            Test("Exists with Literal Boolean Flag Test", q.Expression, expectedResult);
        }

        [TestMethod()]
        public void Sub_query_join_with_Lambda_Parameter_as_scalar_result()
        {
            var students = new Queryable<Student>(queryProvider);
            var studentAttendances = new Queryable<StudentAttendance>(queryProvider);
            var studentGrades = new Queryable<StudentGrade>(queryProvider);
            var q = students
                        .Select(x => x.StudentId)
                        .InnerJoin(studentAttendances.GroupBy(y => y.StudentId).Select(y => y.Key), (s, sa) => new { s, sa }, x => x.s == x.sa)
                        .Select(x => new { Student_StudentID = x.s, StudentAttendance_StudentID = x.sa })
                        .Select(x => x.Student_StudentID)
                        .Select(x => x)
                        .Where(x => studentGrades.Select(y => y.StudentId).Where(y => y == x).Any())
                        ;
            string expectedResult = @"
select a_7.Col1 as Col1
from (
		select a_6.Col1 as Col1
		from (
				select a_5.Student_StudentID as Col1
				from (
						select a_2.Col1 as Student_StudentID, a_3.Col1 as StudentAttendance_StudentID
						from (
								select a_1.StudentId as Col1
								from Student as a_1
							) as a_2
								inner join (
									select a_4.StudentId as Col1
									from StudentAttendance as a_4
									group by a_4.StudentId
								) as a_3 on (a_2.Col1 = a_3.Col1)
					) as a_5
			) as a_6
	) as a_7
where exists(
		select 1 as Col1
		from (
				select a_8.StudentId as Col1
				from StudentGrade as a_8
			) as a_9
		where (a_9.Col1 = a_7.Col1)
	)
";
            
            Test("Sub Query Join with Lambda Parameter as Scalar Result Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void Queryable_selected_in_projection_then_query_wrapped_and_queryable_used_in_Where()
        {
            var employees = new Queryable<Employee>(queryProvider);
            var employeeDegrees = new Queryable<EmployeeDegree>(queryProvider);

            var q = employees
                .Select(e => new
                {
                    e.Name,
                    EmployeeId = "abc"
                    ,
                    DegreeGroups = e.NavDegrees                                     // this sub-query will be translated to outer-apply
                        .GroupBy(d => d.University)
                        .Select(g => new { University = g.Key, TotalDegrees = g.Count() })

                })
                .Where(x => x.DegreeGroups.Any(y => y.TotalDegrees > 0))
                .Where(x => x.DegreeGroups.Any(y => y.University == "123"))
                .Top(50)
                .Where(x => x.DegreeGroups.Where(z => z.TotalDegrees == 10).Where(z => z.University == "123").Any())
                .Where(x => x.DegreeGroups.Where(z => z.University == "56").Any());

            string expectedResult = @"
  	select a_6.Name as Name, a_6.EmployeeId as EmployeeId, a_6.SubQueryCol1 as SubQueryCol1, Queryable: {
		(
				select a_3.University as University, Count(1) as TotalDegrees
				from EmployeeDegree as a_3
				where (a_6.SubQueryCol1 = a_3.EmployeeId)
				group by a_3.University
			)
		} as DegreeGroups
	from (
			select top (50) a_2.Name as Name, a_2.EmployeeId as EmployeeId, a_2.SubQueryCol1 as SubQueryCol1
			from (
					select a_1.Name as Name, 'abc' as EmployeeId, a_1.EmployeeId as SubQueryCol1
					from Employee as a_1
				) as a_2
			where exists(
					select 1 as Col1
					from (
							select a_3.University as University, Count(1) as TotalDegrees
							from EmployeeDegree as a_3
							where (a_2.SubQueryCol1 = a_3.EmployeeId)
							group by a_3.University
						) as a_4
					where (a_4.TotalDegrees > 0)
				)
				 and exists(
					select 1 as Col1
					from (
							select a_3.University as University, Count(1) as TotalDegrees
							from EmployeeDegree as a_3
							where (a_2.SubQueryCol1 = a_3.EmployeeId)
							group by a_3.University
						) as a_5
					where (a_5.University = '123')
				)
		) as a_6
	where exists(
			select 1 as Col1
			from (
					select a_3.University as University, Count(1) as TotalDegrees
					from EmployeeDegree as a_3
					where (a_6.SubQueryCol1 = a_3.EmployeeId)
					group by a_3.University
				) as a_7
			where (a_7.TotalDegrees = 10)
				 and (a_7.University = '123')
		)
		 and exists(
			select 1 as Col1
			from (
					select a_3.University as University, Count(1) as TotalDegrees
					from EmployeeDegree as a_3
					where (a_6.SubQueryCol1 = a_3.EmployeeId)
					group by a_3.University
				) as a_8
			where (a_8.University = '56')
		)
";

            Test("Select with nested GroupBy inside projection should produce correlated subquery", q.Expression, expectedResult);
        }

    }
}