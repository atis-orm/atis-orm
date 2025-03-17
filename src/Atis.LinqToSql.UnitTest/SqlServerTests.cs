// Bismilla-hir-Rahman-nir-Rahim
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
using Atis.Expressions;
using Atis.LinqToSql.ContextExtensions;
using Atis.LinqToSql.SqlExpressions;
using Atis.LinqToSql.Preprocessors;
using Atis.LinqToSql.ExpressionExtensions;

namespace Atis.LinqToSql.UnitTest
{
    [TestClass]
    public class SqlServerTests
    {
        #region base methods
        private void Test(string testHeading, Expression queryExpression, string? expectedResult)
        {
            SqlExpression result = this.ConvertExpressionToSqlExpression(queryExpression);
            var translator = new SqlExpressionTranslator();
            //var xmlDoc2 = SqlExpressionTreeGenerator.GenerateTree(newResult);
            var resultQuery = translator.Translate(result);
            Console.WriteLine($"+++++++++++++++++++++++++ {testHeading} ++++++++++++++++++++++++");
            Console.WriteLine(resultQuery);
            Console.WriteLine("-----------------------------------------------------------------");
            if (expectedResult != null)
            {
                ValidateQueryResults(resultQuery, expectedResult);
            }
        }

        private SqlExpression ConvertExpressionToSqlExpression(Expression queryExpression)
        {
            Console.WriteLine("Original Expression:");
            Console.WriteLine(queryExpression.ToString());
            var updatedQueryExpression = PreprocessExpression(queryExpression);
            Console.WriteLine("Expression after Preprocessing:");
            Console.WriteLine(updatedQueryExpression.ToString());

            //var componentIdentifier = new QueryComponentIdentifier();
            var model = new Model();
            var reflectionService = new ReflectionService(new ExpressionEvaluator());
            var parameterMapper = new LambdaParameterToDataSourceMapper();
            //var navigationMapper = new NavigationToDataSourceMapper();
            //var propertyMapper = new PropertyToDataSourceMapper();
            var contextExtensions = new object[] { /*componentIdentifier,*/ model, parameterMapper, /*navigationMapper,*/ reflectionService/*, propertyMapper*/ };
            var conversionContext = new ConversionContext(contextExtensions);
            var expressionConverterProvider = new LinqToSqlExpressionConverterProvider(conversionContext, factories: null);
            var postProcessorProvider = new PostprocessorProvider(postprocessors: null);
            var linqToSqlConverter = new LinqToSqlConverter(reflectionService, expressionConverterProvider, postProcessorProvider);
            var result = linqToSqlConverter.Convert(updatedQueryExpression);
            return result;
        }

        private Expression PreprocessExpression(Expression expression)
        {
            //var stringLengthReplacementVisitor = new StringLengthReplacementVisitor();
            //expression = stringLengthReplacementVisitor.Visit(expression);
            var queryProvider = new QueryProvider();
            var reflectionService = new ReflectionService(new ExpressionEvaluator());
            var navigateToManyPreprocessor = new NavigateToManyPreprocessor(queryProvider, reflectionService);
            var navigateToOnePreprocessor = new NavigateToOnePreprocessor(queryProvider, reflectionService);
            var queryVariablePreprocessor = new QueryVariableReplacementPreprocessor();
            var childJoinReplacementPreprocessor = new ChildJoinReplacementPreprocessor(reflectionService);
            var calculatedPropertyReplacementPreprocessor = new CalculatedPropertyPreprocessor(reflectionService);
            var specificationPreprocessor = new SpecificationCallRewriterPreprocessor(reflectionService);
            var convertPreprocessor = new ConvertExpressionReplacementPreprocessor();
            //var nonPrimitivePropertyReplacementPreprocessor = new NonPrimitiveCalculatedPropertyPreprocessor(reflectionService);
            //var concreteParameterPreprocessor = new ConcreteParameterReplacementPreprocessor(new QueryPartsIdentifier(), reflectionService);
            var methodInterfaceTypeReplacementPreprocessor = new QueryMethodGenericTypeReplacementPreprocessor(reflectionService);
            var preprocessor = new PreprocessingExpressionVisitor([queryVariablePreprocessor, methodInterfaceTypeReplacementPreprocessor, navigateToManyPreprocessor, navigateToOnePreprocessor, childJoinReplacementPreprocessor, calculatedPropertyReplacementPreprocessor, specificationPreprocessor, convertPreprocessor/*, concreteParameterPreprocessor*/]);
            expression = preprocessor.Preprocess(expression);
            return expression;
        }

        private void ValidateQueryResults(string convertedQuery, string expectedQuery)
        {
            convertedQuery = SimplifyQuery(convertedQuery);
            expectedQuery = SimplifyQuery(expectedQuery);
            if (string.Compare(convertedQuery, expectedQuery, true) != 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("ERROR: Converted query is not as expected.");
                Console.ResetColor();
                Assert.Fail("Query is not matching");
            }
        }

        private string SimplifyQuery(string query)
        {
            query = query.Trim();
            if (query.StartsWith("("))
                query = query.Substring(1, query.Length - 2);
            query = query.Trim();
            query = query.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            while (query.Contains("  "))
            {
                query = query.Replace("  ", " ");
            }
            return query;
        }
        #endregion


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

            var query = new Queryable<Asset>(new QueryProvider());
            if (query is IQueryable<IModelWithItem> queryWithItem)
            {
                var q = queryWithItem.Where(x => x.NavItem().ItemDescription.Contains("abc")).Select(x => x).Where(x => x.NavItem().ItemId == "123");
                var preprocessedExpression = PreprocessExpression(q.Expression);
                Console.WriteLine(q.Expression);
                Console.WriteLine(preprocessedExpression);
            }
        }

        //private class TestVisitor : ExpressionVisitor
        //{
        //    private readonly IQueryPartsIdentifier queryPartsIdentifier = new QueryPartsIdentifier();
        //    private readonly IReflectionService reflectionService = new ReflectionService(new ExpressionEvaluator());
        //    private readonly LinqQueryManager queryManager;
        //    private readonly HashSet<Expression> methodCallChanged = new HashSet<Expression>();

        //    public TestVisitor()
        //    {
        //        this.queryManager = new LinqQueryManager(this.queryPartsIdentifier, this.reflectionService);
        //    }

        //    public override Expression? Visit(Expression? node)
        //    {
        //        if (node is null)
        //            return null;

        //        this.queryManager.EnteringNode(node);
        //        var result = base.Visit(node);
        //        this.queryManager.ExitingNode(node);
        //        return result;
        //    }

        //    protected override Expression VisitMethodCall(MethodCallExpression node)
        //    {
        //        var updatedNode = base.VisitMethodCall(node);

        //        if (updatedNode is MethodCallExpression methodCallExpr && this.queryPartsIdentifier.IsQueryMethod(updatedNode))
        //        {
        //            var firstArg = methodCallExpr.Arguments.FirstOrDefault();
        //            if (firstArg != null)
        //            {
        //                if (this.queryPartsIdentifier.IsQuerySource(firstArg) || methodCallChanged.Contains(firstArg))
        //                {
        //                    var entityType = this.reflectionService.GetEntityTypeFromExpression(firstArg);
        //                    var methodCallEntityType = this.reflectionService.GetEntityTypeFromExpression(methodCallExpr);
        //                    if (methodCallEntityType != entityType)
        //                    {
        //                        var queryMethodReturnTypeReplacer = new QueryMethodReturnTypeReplacer();
        //                        var newMethodCall = queryMethodReturnTypeReplacer.ReplaceType(methodCallExpr, methodCallEntityType, entityType);
        //                        methodCallChanged.Add(newMethodCall);
        //                        return newMethodCall;
        //                    }
        //                }
        //            }
        //        }
        //        return updatedNode;
        //    }
        //}



        private IQueryProvider dbc = new QueryProvider();

        [TestMethod]
        public void T00001_SelectManyChildJoinReplacementTest()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => (x.Degree == "123" || x.Degree == "665") && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            //var selectManyJoinConverter = new ChildJoinReplacementPreprocessor();
            //var updatedExpression = selectManyJoinConverter.Visit(preprocessedExpression);
            Console.WriteLine(updatedExpression);
            if (updatedExpression is MethodCallExpression methodCallExpr &&
                methodCallExpr.Arguments.Skip(1).First() is UnaryExpression unaryExpression &&
                unaryExpression.Operand is LambdaExpression lambdaExpression &&
                lambdaExpression.Body is ChildJoinExpression childJoinCall &&
                childJoinCall.Query is MethodCallExpression childJoinArg1 &&
                childJoinArg1.Method.Name == "Where" &&
                childJoinArg1.Arguments[1] is UnaryExpression childJoinArg1Unary &&
                childJoinArg1Unary.Operand is LambdaExpression childJoinArg1Lambda &&
                childJoinArg1Lambda.Body is BinaryExpression childJoinArg1Binary &&
                childJoinArg1Binary.NodeType == ExpressionType.OrElse
                )
            {
                Console.WriteLine("Success");
            }
            else
            {
                Assert.Fail("Expression was not converted as expected");
            }
        }

        [TestMethod]
        public void T00002_SelectManyChildJoinReplacementOrElseSkipTest()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" || (x.University == "55" && x.RowId == e.RowId)));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if (((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body as MethodCallExpression)?.Method.Name != "Where")
                Assert.Fail("Expression was updated");
            else
                Console.WriteLine("Success");
        }

        [TestMethod]
        public void T00003_SelectManyChildJoinReplacementSourceParamUsedInNonWhereTest()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Select(x => new { x.EmployeeId, x.Degree, e.Name, x.RowId }).Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if (((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body as MethodCallExpression)?.Method.Name != "Where")
                Assert.Fail("Expression was updated");
            else
                Console.WriteLine("Success");
        }

        [TestMethod]
        public void T00004_SelectManyChildJoinReplacementSelectInStartTest()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = employees.SelectMany(e => employeeDegrees.Select(x => new { x.EmployeeId, x.Degree, x.RowId }).Where(x => x.EmployeeId == e.EmployeeId).Where(x => x.Degree == "123" && x.RowId == e.RowId));
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if ((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body is ChildJoinExpression)
                Console.WriteLine("Success");
            else
                Assert.Fail("Expression was not updated");
        }

        [TestMethod]
        public void T00005_SelectManyChildJoinQuerySyntaxTest()
        {
            var employees = new Queryable<Employee>(this.dbc);
            var employeeDegrees = new Queryable<EmployeeDegree>(this.dbc);
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId)
                    from m in employees.Where(x => x.EmployeeId == e.ManagerId)
                    select new { e.Name, ed.Degree, ManagerName = m.Name };
            Console.WriteLine(q.Expression);
            var updatedExpression = PreprocessExpression(q.Expression);
            Console.WriteLine(updatedExpression);
            if ((((updatedExpression as MethodCallExpression)?.Arguments?.Skip(1).FirstOrDefault() as UnaryExpression)?.Operand as LambdaExpression)?.Body is ChildJoinExpression)
                Console.WriteLine("Success");
            else
                Assert.Fail("Expression was not updated");
        }



        [TestMethod]
        public void T00009_ModelPathEqualsTest()
        {
            var m1 = new ModelPath("a");
            var m2 = new ModelPath("a");
            if (!m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath("a");
            m2 = new ModelPath("b");
            if (m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath(path: null);
            m2 = new ModelPath(path: null);
            if (!m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath("a.b");
            m2 = new ModelPath("a.b");
            if (!m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath("b.a");
            m2 = new ModelPath("a.b");
            if (m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");
        }


        [TestMethod]
        public void T00010_SimpleTest()
        {
            var q = new Queryable<Student>(dbc);
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1";
            Test("Simple Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00020_WhereTest()
        {
            var q = new Queryable<Student>(dbc).Where(x => x.StudentId == "123");
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.StudentId = '123')";
            Test("Where Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00030_SelectTest()
        {
            var q = new Queryable<Student>(dbc).Select(x => new { x.StudentId, x.Name });
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
from	Student as a_1";
            Test("Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00040_FullSimpleQueryTest()
        {
            var q = new Queryable<Student>(dbc)
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
select	top (5)	a_1.StudentId as Id, a_1.Address as Address, len(a_1.Address) as AddressLength, a_1.Age as Age
from	Student as a_1
where	(a_1.StudentId = '55')
	    and	(a_1.Name like ('%' + ('Jhon' + '%')))
order by Id asc";
            Test("Full Simple Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00050_ExpressionInOrderByTest()
        {
            var q = new Queryable<Student>(dbc)
                .Select(x => new { Id = x.StudentId })
                .OrderBy(x => x.Id + "3")
                .OrderByDescending(x => x.Id)
                .Take(5);
            var expectedResult = @"
select	top (5)	a_1.StudentId as Id
from	Student as a_1
order by (a_1.StudentId + '3') asc, Id desc";
            Test("Expression In OrderBy Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00060_SimpleSubQueryTest()
        {
            var q = new Queryable<Student>(dbc)
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
        public void T00070_ComplexSubQueryTest()
        {
            var q = new Queryable<Student>(dbc)
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
	where	(lower(a_2.Address) like ('%' + ('US' + '%')))";
            Test("Complex Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00080_OtherTableQueryTest()
        {
            var q = new Queryable<Student>(dbc)
                .Select(x => new
                {
                    Id = x.StudentId,
                    Grd = dbc.DataSet<StudentGrade>().Where(y => y.StudentId == x.StudentId).Select(y => y.Grade).FirstOrDefault()
                })
                .Where(x => dbc.DataSet<StudentGrade>().Where(y => y.StudentId == x.Id).Any());
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
	select	1
	from	StudentGrade as a_4
	where	(a_4.StudentId = a_3.Id)
)";
            Test("Other Table Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00090_ComplexNestedFirstOrDefaultTest()
        {
            Expression<Func<object>> queryExpression = () => dbc.DataSet<Student>()
                .Select(x => new { Id = x.StudentId })
                .FirstOrDefault(
                        x => x.Id == "123" &&
                        dbc.DataSet<StudentGrade>()
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

        [TestMethod]
        public void T00100_ParameterInOrderByTest()
        {
            var q = new Queryable<Student>(dbc).Select(x => x.StudentId).OrderBy(t => t).OrderBy(u => u + "123");
            var expectedResult = @"
select  a_1.StudentId as Col1
from    Student as a_1
order by Col1 asc, (a_1.StudentId + '123') asc";
            Test("Parameter In OrderBy", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00110_ComplexOrderByTest()
        {
            var q = new Queryable<Student>(dbc)
                                    .Select(x => x.StudentId)
                                    .Where(x => x == "5")
                                    .OrderBy(x => x + "3")
                                    .OrderByDescending(x => x)
                                    .Take(5);
            var expectedResult = @"
select  top (5) a_2.Col1 as Col1
from    (
        select  a_1.StudentId as Col1
        from    Student as a_1
) as a_2
where   (a_2.Col1 = '5')
order by (a_2.Col1 + '3') asc, a_2.Col1 desc";
            Test("Complex Order By Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00120_MultipleDataSourcesTest()
        {
            var q = dbc.From(() => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() });
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		cross join EmployeeDegree as a_2
";
            Test("Multiple Data Sources Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00130_MultipleSubQueryDataSourcesTest()
        {
            var q = dbc.From(() => new
            {
                s = dbc.DataSet<Student>().Where(x => x.Address.Contains("KHI")).Schema(),
                sg = dbc.DataSet<StudentGrade>().Where(x => x.Grade == "5").Schema(),
            });
            var expectedResult = @$"
select	a_2.StudentId as StudentId, a_2.Name as Name, a_2.Address as Address, a_2.Age as Age, a_2.AdmissionDate as AdmissionDate, a_2.RecordCreateDate as RecordCreateDate, a_2.RecordUpdateDate as RecordUpdateDate, a_2.StudentType as StudentType, a_2.CountryID as CountryID, a_2.HasScholarship as HasScholarship, a_4.RowId as RowId, a_4.StudentId as StudentId_1, a_4.Grade as Grade
	from	(
		select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
		from	Student as a_1
		where	(a_1.Address like ('%' + ('KHI' + '%')))
	) as a_2
		cross join (
			select	a_3.RowId as RowId, a_3.StudentId as StudentId, a_3.Grade as Grade
			from	StudentGrade as a_3
			where	(a_3.Grade = '5')
		) as a_4";
            Test("Multiple Sub Query Data Sources Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00140_MultipleSubQueryDataSourcesComplexTest()
        {
            var q =
            dbc.From(() => new
            {
                s = dbc.DataSet<Student>().Where(x => x.Address.Contains("KHI")).Schema(),
                sg = QueryExtensions.Table<StudentGrade>(),
            })
            .Where(p1 => p1.s.StudentId == p1.sg.StudentId)
            .Take(5)
            .OrderBy(x => x.s.StudentId)
            .Select(x => new { x.s.StudentId, x.sg.Grade, SgStudentId = x.sg.StudentId });
            ;
            var expectedResult = @$"
select	a_4.StudentId as StudentId, a_4.Grade as Grade, a_4.StudentId_1 as SgStudentId
	from	(
		select	top (5)	a_2.StudentId as StudentId, a_2.Name as Name, a_2.Address as Address, a_2.Age as Age, a_2.AdmissionDate as AdmissionDate, a_2.RecordCreateDate as RecordCreateDate, a_2.RecordUpdateDate as RecordUpdateDate, a_2.StudentType as StudentType, a_2.CountryID as CountryID, a_2.HasScholarship as HasScholarship, a_3.RowId as RowId, a_3.StudentId as StudentId_1, a_3.Grade as Grade
		from	(
			select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
			from	Student as a_1
			where	(a_1.Address like ('%' + ('KHI' + '%')))
		) as a_2
			cross join StudentGrade as a_3
		where	(a_2.StudentId = a_3.StudentId)
	) as a_4
	order by a_4.StudentId asc
";
            Test("Multiple Sub Query Data Sources Complex Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00150_SimpleJoinTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .LeftJoin(dbc.DataSet<StudentGrade>(), (s, sg) => new { s, sg }, j => j.s.StudentId == j.sg.StudentId)
            .LeftJoin(dbc.DataSet<StudentGradeDetail>(), (ps, gd) => new { s1 = ps.s, sg1 = ps.sg, gd }, j2 => j2.sg1.RowId == j2.gd.StudentGradeRowId)
            ;
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.RowId as RowId, a_2.StudentId as StudentId_1, a_2.Grade as Grade, a_3.RowId as RowId_1, a_3.StudentGradeRowId as StudentGradeRowId, a_3.SubjectId as SubjectId, a_3.MarksGained as MarksGained, a_3.TotalMarks as TotalMarks
	from	Student as a_1
		left join StudentGrade as a_2 on (a_1.StudentId = a_2.StudentId)
		left join StudentGradeDetail as a_3 on (a_2.RowId = a_3.StudentGradeRowId)
";
            Test("Simple Join Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00160_JoinWithNestedDataSourcesTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .LeftJoin(dbc.DataSet<StudentGrade>(), (s, sg) => new { s, sg }, j => j.s.StudentId == j.sg.StudentId)
            .LeftJoin(dbc.DataSet<StudentGradeDetail>(), (ps, gd) => new { ps, gd }, j2 => j2.ps.sg.RowId == j2.gd.StudentGradeRowId)
            ;
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.RowId as RowId, a_2.StudentId as StudentId_1, a_2.Grade as Grade, a_3.RowId as RowId_1, a_3.StudentGradeRowId as StudentGradeRowId, a_3.SubjectId as SubjectId, a_3.MarksGained as MarksGained, a_3.TotalMarks as TotalMarks
	from	Student as a_1
		left join StudentGrade as a_2 on (a_1.StudentId = a_2.StudentId)
		left join StudentGradeDetail as a_3 on (a_2.RowId = a_3.StudentGradeRowId)
";
            Test("Join Nested Data Sources Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00170_JoinWithSubQueryTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .LeftJoin(dbc.DataSet<StudentGrade>(), (s, sg) => new { s, sg }, j => j.s.StudentId == j.sg.StudentId)
            .LeftJoin(dbc.DataSet<StudentGradeDetail>().Where(x => x.TotalMarks > 50).Select(x => new { SGRID = x.StudentGradeRowId, TM = x.TotalMarks }), (ps, gd) => new { ps, gd }, j2 => j2.ps.sg.RowId == j2.gd.SGRID)
            ;
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.RowId as RowId, a_2.StudentId as StudentId_1, a_2.Grade as Grade, a_4.SGRID as SGRID, a_4.TM as TM
	from	Student as a_1
		left join StudentGrade as a_2 on (a_1.StudentId = a_2.StudentId)
		left join (
			select	a_3.StudentGradeRowId as SGRID, a_3.TotalMarks as TM
			from	StudentGradeDetail as a_3
			where	(a_3.TotalMarks > 50)
		) as a_4 on (a_2.RowId = a_4.SGRID)
";
            Test("Join With Sub Query Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00180_WrapOnJoinTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .LeftJoin(dbc.DataSet<StudentGrade>(), (s, sg) => new { s, sg }, j => j.s.StudentId == j.sg.StudentId)
            .Select(x => new { x.s.StudentId, x.s.Name, StudentGradeRowId = x.sg.RowId, x.sg.Grade })
            .LeftJoin(dbc.DataSet<StudentGradeDetail>().Where(x => x.TotalMarks > 50).Select(x => new { SGRID = x.StudentGradeRowId, TM = x.TotalMarks }), (ps, gd) => new { ps, gd }, j2 => j2.ps.StudentGradeRowId == j2.gd.SGRID)
            ;
            var expectedResult = @"
select	a_3.StudentId as StudentId, a_3.Name as Name, a_3.StudentGradeRowId as StudentGradeRowId, a_3.Grade as Grade, a_5.SGRID as SGRID, a_5.TM as TM
	from	(
		select	a_1.StudentId as StudentId, a_1.Name as Name, a_2.RowId as StudentGradeRowId, a_2.Grade as Grade
		from	Student as a_1
			left join StudentGrade as a_2 on (a_1.StudentId = a_2.StudentId)
	) as a_3
		left join (
			select	a_4.StudentGradeRowId as SGRID, a_4.TotalMarks as TM
			from	StudentGradeDetail as a_4
			where	(a_4.TotalMarks > 50)
		) as a_5 on (a_3.StudentGradeRowId = a_5.SGRID)
";
            Test("Wrap on Join Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00190_MultiDataSourceJoinTest()
        {
            var q =
            dbc.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>(),
            })
            .LeftJoin(x => x.sg, j => j.s.StudentId == j.sg.StudentId)
            .LeftJoin(dbc.DataSet<StudentGradeDetail>(), (os, gd) => new { os, gd }, j2 => j2.os.sg.RowId == j2.gd.StudentGradeRowId)
            ;
            var expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.RowId as RowId, a_2.StudentId as StudentId_1, a_2.Grade as Grade, a_3.RowId as RowId_1, a_3.StudentGradeRowId as StudentGradeRowId, a_3.SubjectId as SubjectId, a_3.MarksGained as MarksGained, a_3.TotalMarks as TotalMarks
	from	Student as a_1
		left join StudentGrade as a_2 on (a_1.StudentId = a_2.StudentId)
		left join StudentGradeDetail as a_3 on (a_2.RowId = a_3.StudentGradeRowId)
";
            Test("Multi Data Source Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00200_GroupByTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .GroupBy(x => new { G1 = x.Address, G2 = x.Age })
            .Select(x => new
            {
                x.Key.G1,
                x.Key.G2,
                MaxStudentId = x.Max(y => y.StudentId),
                TotalLines = x.Count(),
                CL = dbc.DataSet<StudentGrade>().Where(y => y.StudentId == x.Max(z => z.StudentId)).Select(y => y.Grade).FirstOrDefault()
            })
            ;
            string? expectedResult = @"
select	a_1.Address as G1, a_1.Age as G2, Max(a_1.StudentId) as MaxStudentId, Count(1) as TotalLines, (
		select	top (1)	a_2.Grade as Col1
		from	StudentGrade as a_2
		where	(a_2.StudentId = Max(a_1.StudentId))
	) as CL
	from	Student as a_1
	group by a_1.Address, a_1.Age
";
            Test("Group By Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00210_GroupByScalarTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .GroupBy(x => x.Address)
            .Select(x => new { Add = x.Key, TotalLines = x.Count(), MaxLine = x.Max(y => y.StudentId) });
            string? expectedResult = @"
select  a_1.Address as Add, count(1) as TotalLines, max(a_1.StudentId) as MaxLine
from    Student as a_1
group by a_1.Address
";
            Test("Group By Scalar Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00220_DataSourceParameterSelectionTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .Where(x => x.StudentId == "123")
            .Select(x => x);
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.StudentId = '123')";
            Test("Data Source Parameter Selection Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00230_FullQueryInSelectWithSingleColumn()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .Where(x => x.StudentId == "123")
            .Select(x => new { Std = x, STD_ID = x.StudentId });
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_1.StudentId as STD_ID
	from	Student as a_1
	where	(a_1.StudentId = '123')";
            Test("Data Source Parameter Selection Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00240_DataSourceSelectedAsWholeInMultiDataSourceQueryTest()
        {
            var q =
            dbc.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>()
            })
            .Where(x => x.s.StudentId == "123")
            .Select(x => new { x.s, x.sg.Grade });
            string? expectedResult = @$"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.Grade as Grade
	from	Student as a_1
		cross join StudentGrade as a_2
	where	(a_1.StudentId = '123')
";
            Test("Data Source Selected As Whole In Multi Data Source Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00250_OnlyDataSourceSelectedAsWholeInMultiDataSourceQueryTest()
        {
            var q =
            dbc.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>()
            })
            .Where(x => x.s.StudentId == "123")
            .Select(x => x.s);
            string? expectedResult = @$"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
		cross join StudentGrade as a_2
	where	(a_1.StudentId = '123')
";
            Test("Data Source Selected As Whole In Multi Data Source Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00260_ParameterSelectionInMultiDataSourceQueryTest()
        {
            var q =
            dbc.From(() => new
            {
                s = QueryExtensions.Table<Student>(),
                sg = QueryExtensions.Table<StudentGrade>()
            })
            .Where(x => x.s.StudentId == "123")
            .Select(f => f);
            string? expectedResult = @$"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship, a_2.RowId as RowId, a_2.StudentId as StudentId_1, a_2.Grade as Grade
	from	Student as a_1
		cross join StudentGrade as a_2
	where	(a_1.StudentId = '123')
";
            Test("Parameter Selection In Multi Data Source Query Test", q.Expression, expectedResult);
        }

        private class StudentQueryResult
        {
            public string? StudentId { get; set; }
            public string? StudentName { get; set; }
            public string? Grade { get; set; }
        }

        [TestMethod]
        public void T00270_MemberInitTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<Student>()
            .Select(x => new StudentQueryResult
            {
                StudentId = x.StudentId,
                StudentName = x.Name,
                Grade = dbc.DataSet<StudentGrade>().Where(y => y.StudentId == x.StudentId).Select(y => y.Grade).FirstOrDefault()
            });
            string? expectedResult = @"
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
        public void T00280_GroupJoinOnScalarSelectTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<StudentGrade>()
            .GroupBy(x => x.StudentId)
            .Select(x => x.Key)
            .LeftJoin(dbc.DataSet<Student>(), (g, s) => new { g, s }, j => j.g == j.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as Col1, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (a_2.Col1 = a_3.StudentId)
";
            Test("Group Join On Scalar Select Test", queryExpression.Body, expectedResult);
        }


        [TestMethod]
        public void T00290_GroupJoinOnScalarSelectWithFunctionTest()
        {
            Expression<Func<object>> queryExpression = () =>
            dbc.DataSet<StudentGrade>()
            .GroupBy(x => x.StudentId)
            .Select(x => x.Key)
            .LeftJoin(dbc.DataSet<Student>(), (g, s) => new { g, s }, j => j.g.Substring(0, 5) == j.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as Col1, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (substring(a_2.Col1, (0 + 1), 5) = a_3.StudentId)
";
            Test("Group Join On Scalar Select Test", queryExpression.Body, expectedResult);
        }

        [TestMethod]
        public void T00300_GroupJoinMultipleDataSourceTest()
        {
            var q =
            dbc.From(() => new
            {
                g = dbc.DataSet<StudentGrade>().GroupBy(x => x.StudentId).Select(x => x.Key).Schema(),
                s = QueryExtensions.Table<Student>(),
            })
            .LeftJoin(x => x.s, x => x.g == x.s.StudentId);
            string? expectedResult = @"
select	a_2.Col1 as Col1, a_3.StudentId as StudentId, a_3.Name as Name, a_3.Address as Address, a_3.Age as Age, a_3.AdmissionDate as AdmissionDate, a_3.RecordCreateDate as RecordCreateDate, a_3.RecordUpdateDate as RecordUpdateDate, a_3.StudentType as StudentType, a_3.CountryID as CountryID, a_3.HasScholarship as HasScholarship
	from	(
		select	a_1.StudentId as Col1
		from	StudentGrade as a_1
		group by a_1.StudentId
	) as a_2
		left join Student as a_3 on (a_2.Col1 = a_3.StudentId)
";
            Test("Group Join Multiple Data Source Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00310_RelationNavigationTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Equipment>()
            .Where(x => x.NavItem().UnitPrice > 500)
            .Select(x => new { ItemId = x.NavItem().ItemId, UnitPrice = x.NavItem().UnitPrice, x.EquipId })
            //.Select(x => new { ItemId = LinqToSql.QueryExtensions.Nav<Equipment, ItemExtension, decimal?>(x, "NavItemId", dbc.DataSet<ItemExtension>(), param0 => param0.UnitPrice, SqlExpressions.SqlJoinType.Left, otherEntity => x.ItemId == otherEntity.ItemId) })
            ;



            string? expectedResult = @"
select	NavItem_2.ItemId as ItemId, NavItem_2.UnitPrice as UnitPrice, a_1.EquipId as EquipId
	from	Equipment as a_1
		left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	where	(NavItem_2.UnitPrice > 500)";
            Test("Relation Navigation Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void T00311_RelationNavigationToCustomTypeTest()
        {
            var inventoryTransactions = new Queryable<ItemInventoryTransaction>(new QueryProvider());
            var q = inventoryTransactions.Select(x => new { x.TransactionId, x.ItemId, x.NavSummaryLine().TotalCapturedQty, x.NavSummaryLine().TotalQtyGained, x.NavSummaryLine().TotalQtyLost });
            string? expectedResult = @"
select	a_1.TransactionId as TransactionId, a_1.ItemId as ItemId, NavSummaryLine_3.TotalCapturedQty as TotalCapturedQty, NavSummaryLine_3.TotalQtyGained as TotalQtyGained, NavSummaryLine_3.TotalQtyLost as TotalQtyLost
	from	ItemInventoryTransaction as a_1
		left join (
			select	a_2.TransactionRowId as TransactionRowId, Sum(a_2.CapturedQty) as TotalCapturedQty, Sum(case when (a_2.NewQty > a_2.CapturedQty) then (a_2.NewQty - a_2.CapturedQty) else 0 end) as TotalQtyGained, Sum(case when (a_2.CapturedQty > a_2.NewQty) then (a_2.CapturedQty - a_2.NewQty) else 0 end) as TotalQtyLost
			from	ItemInventoryTransactionDetail as a_2
			group by a_2.TransactionRowId
		) as NavSummaryLine_3 on (a_1.RowId = NavSummaryLine_3.TransactionRowId)
";

            Test("Relation Navigation To Custom Type Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00312_RelationAttributeTest()
        {
            var inventoryTransactionLines = new Queryable<ItemInventoryTransactionDetail>(new QueryProvider());
            var q = inventoryTransactionLines.Select(x => new { x.NavParentTransaction().TransactionId, x.NavParentTransaction().ItemId, x.CapturedQty, x.NewQty, x.LineStatus });
            string? expectedResult = @"
select	NavParentTransaction_2.TransactionId as TransactionId, NavParentTransaction_2.ItemId as ItemId, a_1.CapturedQty as CapturedQty, a_1.NewQty as NewQty, a_1.LineStatus as LineStatus
	from	ItemInventoryTransactionDetail as a_1
		inner join ItemInventoryTransaction as NavParentTransaction_2 on (NavParentTransaction_2.RowId = a_1.TransactionRowId)
";

            Test("Relation Attribute Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00320_RelationOverSubqueryTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Equipment>()
            .Take(50)
            .Select(x => new { ItemId = x.NavItem().ItemId })
            ;

            string? expectedResult = @"
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
        public void T00330_RelationFullSelectionTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Equipment>()
            .Select(x => x.NavItem())
            ;

            string? expectedResult = @"
select	NavItem_2.ItemId as ItemId, NavItem_2.UnitPrice as UnitPrice
	from	Equipment as a_1
		left join ItemExtension as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
";
            Test("Relation Full Selection Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void T00340_RelationFullSelectionThenSubQueryTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Equipment>()
            .Select(x => x.NavItem())
            .Select(x => x.UnitPrice)
            ;

            string? expectedResult = @"
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
        public void T00350_ComplexNavigationRelationInnerJoinCreatedAsLeftJoinTest()
        {
            // in this test we are seeing that NavItem in Equipment entity is parent optional
            // so the join will be left join, then we see that from NavItem to NavItemBase is
            // 1 to 1 relation but parent NOT optional, which should create inner join but
            // since the first join is left, so later joins will be left join as well

            var equipmentList = new Queryable<Equipment>(this.dbc);
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

            string? expectedResult = @"
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
        public void T00360_NavigationPlusJoinTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Component>()
            .LeftJoin(dbc.DataSet<ItemBase>(), (c, i) => new { c, i }, j => j.c.ItemId == j.i.ItemId)
            .LeftJoin(dbc.DataSet<Equipment>(), (ds, e) => new { ds, e }, j => j.ds.c.EquipId == j.e.EquipId)
            .Select(x => new { x.ds.c.CompId, CompItem = x.ds.c.ItemId, x.ds.c.NavItem.UnitPrice, x.ds.c.EquipId, x.e.Model, EquipItemDesc = x.e.NavItem().NavItemBase().ItemDescription })
            ;

            string? expectedResult = @"
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
        public void T00370_CrossApplyTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Equipment>()
            .CrossApply(x1 => dbc.DataSet<ItemBase>().Where(y => y.ItemId == x1.ItemId).Take(1), (e, i) => new { e, i })
            .Select(x2 => new { x2.e.EquipId, x2.i.ItemDescription })
            ;

            string? expectedResult = @"
select  a_1.EquipId as EquipId, a_3.ItemDescription as ItemDescription
from    Equipment as a_1
        cross apply (
                select  top (1) a_2.ItemId as ItemId, a_2.ItemDescription as ItemDescription
                from    ItemBase as a_2
                where   (a_2.ItemId = a_1.ItemId)
        ) as a_3
";
            Test("Cross Apply Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void T00380_OuterApplyTest()
        {
            Expression<Func<object>> temp = () =>
            dbc.DataSet<Equipment>()
            .OuterApply(x => dbc.DataSet<ItemBase>().Where(y => y.ItemId == x.ItemId).Take(1), (e, i) => new { e, i })
            .Select(x => new { x.e.EquipId, x.i.ItemDescription })
            ;

            string? expectedResult = @"
select  a_1.EquipId as EquipId, a_3.ItemDescription as ItemDescription
from    Equipment as a_1
        outer apply (
                select  top (1) a_2.ItemId as ItemId, a_2.ItemDescription as ItemDescription
                from    ItemBase as a_2
                where   (a_2.ItemId = a_1.ItemId)
        ) as a_3
";
            Test("Outer Apply Test", temp.Body, expectedResult);
        }

        [TestMethod]
        public void T00390_QueryVariableTest()
        {
            IQueryable<Student> queryStudent = new Queryable<Student>(new QueryProvider());
            IQueryable<StudentGrade> queryGrade = new Queryable<StudentGrade>(new QueryProvider()).Where(x => x.Grade == "5");
            queryStudent = queryStudent.Where(x => x.Name.Contains("Abc"));
            queryStudent = queryStudent.Where(x => queryGrade.Any(y => y.StudentId == x.StudentId));

            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Name like ('%' + ('Abc' + '%'))) and exists(
		select	1
		from	StudentGrade as a_2
		where	(a_2.Grade = '5') and (a_2.StudentId = a_1.StudentId)
	)
";
            Test("Query Variable Test", queryStudent.Expression, expectedResult);
        }

        [TestMethod]
        public void T00400_HavingTest()
        {
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .GroupBy(x => x.Name)
                        .Where(x => x.Max(y => y.Age) > 20)
                        .Select(x => new { Name = x.Key, TotalLines = x.Count() })
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, Count(1) as TotalLines
from	Student as a_1
where	(a_1.Address like ('%' + ('City' + '%')))
group by a_1.Name
having	(Max(a_1.Age) > 20)";
            Test("Having Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00410_HavingWithoutGroupByTest()
        {
            var s = new Queryable<Student>(new QueryProvider());
            var studentGrades = new Queryable<StudentGrade>(new QueryProvider());
            var q = s.Where(x => studentGrades.Where(y => y.StudentId == x.StudentId).Having(y => y.Count() > 1).Any());

            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	exists(
		select	1
		from	StudentGrade as a_2
		where	(a_2.StudentId = a_1.StudentId)
		having	(Count(1) > 1)
	)";
            Test("Having without Group By Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00420_PagingTest()
        {
            IQueryable<Student> students = new Queryable<Student>(new QueryProvider());

            var q = students.Where(x => x.Address.Contains("City"))
                            .OrderBy(x => x.Name)
                            .Select(x => new { x.Name, Id = x.StudentId })
                            .Paging(2, 10);

            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like ('%' + ('City' + '%')))
order by a_1.Name asc
offset 10 rows fetch next 10 rows only";
            Test("Paging Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00430_PagingWithSkipAndTakeTest()
        {
            int pageNumber = 5;
            int pageSize = 10;
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .OrderBy(x => x.Name)
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like ('%' + ('City' + '%')))
order by a_1.Name asc
offset 40 rows fetch next 10 rows only";
            Test("Paging With Skip And Take Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00440_PagingWithoutOrderByTest()
        {
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .Paging(2, 10)
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like ('%' + ('City' + '%')))
order by 1 asc
offset 10 rows fetch next 10 rows only";
            Test("Paging Without OrderBy Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00450_UnionTest()
        {
            IQueryable<Student> s = new Queryable<Student>(new QueryProvider());
            var q = s.Where(x => x.Address.Contains("City"))
                        .Select(x => new { x.Name, Id = x.StudentId })
                        .UnionAll(s.Where(x => x.Address.Contains("Town")).Select(x => new { x.Name, Id = x.StudentId }))
                        ;
            string? expectedResult = @"
select	a_1.Name as Name, a_1.StudentId as Id
from	Student as a_1
where	(a_1.Address like ('%' + ('City' + '%')))
union all
select	a_2.Name as Name, a_2.StudentId as Id
from	Student as a_2
where	(a_2.Address like ('%' + ('Town' + '%')))";
            Test("Union Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00460_RecursiveAnchorAfterTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .InnerJoin(anchorSource,
                                                            (recursiveMember, anchorMember) => new { anchor = anchorMember, recursive = recursiveMember },
                                                            newShape => newShape.anchor.EmployeeId == newShape.recursive.ManagerId)
                                                .Select(newShape => new { newShape.recursive.EmployeeId, newShape.recursive.Name, newShape.recursive.ManagerId }))
                        ;
            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
		inner join cte_1 as a_4 on (a_4.EmployeeId = a_3.ManagerId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
";
            Test("Recursive Anchor After Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00470_RecursiveAnchorBeforeTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => anchorSource
                                                .InnerJoin(employees,
                                                            (anchorMember, recursiveMember) => new { anchor = anchorMember, recursive = recursiveMember },
                                                            newShape => newShape.anchor.EmployeeId == newShape.recursive.ManagerId)
                                                .Select(newShape => new { newShape.recursive.EmployeeId, newShape.recursive.Name, newShape.recursive.ManagerId }))
                        ;
            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_4.EmployeeId as EmployeeId, a_4.Name as Name, a_4.ManagerId as ManagerId	
	from	cte_1 as a_3	
		inner join Employee as a_4 on (a_3.EmployeeId = a_4.ManagerId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
";
            Test("Recursive Before Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00480_RecursiveAnchorInExistsTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .Where(o1 => anchorSource.Any(y => y.EmployeeId == o1.ManagerId))
                                                .Select(o2 => new { o2.EmployeeId, o2.Name, o2.ManagerId }))
                        ;
            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
	where	exists(	
		select	1	
		from	cte_1 as a_4	
		where	(a_4.EmployeeId = a_3.ManagerId)	
	)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
";
            Test("Recursive Anchor In Exists Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00490_RecursiveUnionWithSubQueryTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .Where(o1 => anchorSource.Any(y => y.EmployeeId == o1.ManagerId))
                                                .Select(o2 => new { o2.EmployeeId, o2.Name, o2.ManagerId }))
                        //.Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .InnerJoin(employeeDegrees, (emp, deg) => new { emp, deg }, j => j.emp.EmployeeId == j.deg.EmployeeId)
                        .Select(x => new
                        {
                            x.emp.EmployeeId,
                            x.emp.Name,
                            x.emp.ManagerId,
                            x.deg.Degree,
                            x.deg.University,
                            f = employees.Where(u1 => u1.ManagerId == null)
                                        .Select(u1 => new { u1.EmployeeId, u1.ManagerId })
                                        .RecursiveUnion(as1 => employees.InnerJoin(as1, (e1, as1) => new { anchor1 = as1, recursive1 = e1 }, j1 => j1.anchor1.EmployeeId == j1.recursive1.ManagerId)
                                                                        .Select(s1 => new { s1.recursive1.EmployeeId, s1.recursive1.ManagerId }))
                                        .Select(s2 => s2.ManagerId)
                                        .FirstOrDefault()
                        })
                        ;
            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
	where	exists(	
		select	1	
		from	cte_1 as a_4	
		where	(a_4.EmployeeId = a_3.ManagerId)	
	)	
), cte_5 as 
(	
	select	a_6.EmployeeId as EmployeeId, a_6.ManagerId as ManagerId	
	from	Employee as a_6	
	where	a_6.ManagerId is null	
	union all	
	select	a_7.EmployeeId as EmployeeId, a_7.ManagerId as ManagerId	
	from	Employee as a_7	
		inner join cte_5 as a_8 on (a_8.EmployeeId = a_7.ManagerId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId, a_9.Degree as Degree, a_9.University as University, (
	select	top (1)	cte_5.ManagerId as Col1
	from	cte_5 as cte_5
) as f
from	cte_1 as cte_1
	inner join EmployeeDegree as a_9 on (cte_1.EmployeeId = a_9.EmployeeId)
";
            Test("Recursive Union With Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00491_RecursiveUnionInnerSubQueryTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());

            var q = employees
                        .Where(x => x.ManagerId == null)
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId })
                        .RecursiveUnion(
                            anchorSource => employees
                                                .Where(o1 => anchorSource.Any(y => y.EmployeeId == o1.ManagerId))
                                                .Select(o2 => new { o2.EmployeeId, o2.Name, o2.ManagerId }))
                        .InnerJoin(employeeDegrees, (emp, deg) => new { emp, deg }, j => j.emp.EmployeeId == j.deg.EmployeeId)
                        .Select(x => new
                        {
                            x.emp.EmployeeId,
                            x.emp.Name,
                            x.emp.ManagerId,
                            x.deg.Degree,
                            x.deg.University,
                            f = employees.Where(u1 => u1.ManagerId == null)
                                        .Select(u1 => new { u1.EmployeeId, u1.ManagerId })
                                        .RecursiveUnion(as1 => employees.InnerJoin(as1, (e1, as1) => new { anchor1 = as1, recursive1 = e1 }, j1 => j1.anchor1.EmployeeId == j1.recursive1.ManagerId)
                                                                        .Select(s1 => new { s1.recursive1.EmployeeId, s1.recursive1.ManagerId }))
                                        .Select(s2 => s2.ManagerId)
                                        .FirstOrDefault()
                        });


            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
	where	exists(	
		select	1	
		from	cte_1 as a_4	
		where	(a_4.EmployeeId = a_3.ManagerId)	
	)	
), cte_5 as 
(	
	select	a_6.EmployeeId as EmployeeId, a_6.ManagerId as ManagerId	
	from	Employee as a_6	
	where	a_6.ManagerId is null	
	union all	
	select	a_7.EmployeeId as EmployeeId, a_7.ManagerId as ManagerId	
	from	Employee as a_7	
		inner join cte_5 as a_8 on (a_8.EmployeeId = a_7.ManagerId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId, a_9.Degree as Degree, a_9.University as University, (
	select	top (1)	cte_5.ManagerId as Col1
	from	cte_5 as cte_5
) as f
from	cte_1 as cte_1
	inner join EmployeeDegree as a_9 on (cte_1.EmployeeId = a_9.EmployeeId)
";
            Test("Recursive Union With Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00492_RecursiveUnionWithQuerySyntaxTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = (
                    from manager in employees
                    where manager.ManagerId == null
                    select new { manager.EmployeeId, manager.Name, manager.ManagerId }
                    )
                    .RecursiveUnion(anchorSource => from subOrdinate in employees
                                                    join anchorMember in anchorSource on subOrdinate.ManagerId equals anchorMember.EmployeeId
                                                    select new { subOrdinate.EmployeeId, subOrdinate.Name, subOrdinate.ManagerId })
                    .Select(outer => new { outer.EmployeeId, outer.Name, outer.ManagerId });
            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
		inner join cte_1 as a_4 on (a_3.ManagerId = a_4.EmployeeId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
";
            Test("Recursive Union With Query Syntax Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00493_RecursiveUnionWithQuerySyntaxOnSubQueryLevelTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());


            var recursiveQuery = (
                    from manager in employees
                    where manager.ManagerId == null
                    select new { manager.EmployeeId, manager.Name, manager.ManagerId }
                    )
                    .RecursiveUnion(anchorSource => from subordinate in employees
                                                    join anchorMember in anchorSource on subordinate.ManagerId equals anchorMember.EmployeeId
                                                    select new { subordinate.EmployeeId, subordinate.Name, subordinate.ManagerId })
                    .Select(outer => new { outer.EmployeeId, outer.Name, outer.ManagerId });


            var q = from degree in employeeDegrees
                    where recursiveQuery.Any(x => x.EmployeeId == degree.EmployeeId)
                    select degree;


            string? expectedResult = @"
with cte_2 as 
(	
	select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
	where	a_3.ManagerId is null	
	union all	
	select	a_4.EmployeeId as EmployeeId, a_4.Name as Name, a_4.ManagerId as ManagerId	
	from	Employee as a_4	
		inner join cte_2 as a_5 on (a_4.ManagerId = a_5.EmployeeId)	
)
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Degree as Degree, a_1.University as University
from	EmployeeDegree as a_1
where	exists(
	select	1
	from	(
		select	cte_2.EmployeeId as EmployeeId, cte_2.Name as Name, cte_2.ManagerId as ManagerId
		from	cte_2 as cte_2
	) as a_6
	where	(a_6.EmployeeId = a_1.EmployeeId)
)
";

            Test("Recursive Union With Query Syntax on Sub-Query Level Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00494_RecursiveUnionWithNavigationTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());

            var q = employees
                        .Where(x => x.ManagerId == null)
                        .RecursiveUnion(anchor => anchor.SelectMany(anchorMember => anchorMember.NavSubOrdinates))
                        .Select(x => new { x.EmployeeId, x.Name, x.ManagerId });

            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	NavSubOrdinates_4.RowId as RowId, NavSubOrdinates_4.EmployeeId as EmployeeId, NavSubOrdinates_4.Name as Name, NavSubOrdinates_4.Department as Department, NavSubOrdinates_4.ManagerId as ManagerId	
	from	cte_1 as a_3	
		inner join Employee as NavSubOrdinates_4 on (a_3.EmployeeId = NavSubOrdinates_4.ManagerId)	
)
select	cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
";
            Test("Recursive Union With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00495_RecursiveUnionWithSubQueryOuterDataSourceInAnchorTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from employeeDegree in employeeDegrees
                    where employeeDegree.EmployeeId == "123"
                    select new
                    {
                        employeeDegree.EmployeeId,
                        employeeDegree.NavEmployee().Name,
                        FirstManagerStartsWithA =     (
                                                        from subOrdinate in employees
                                                        where subOrdinate.EmployeeId == employeeDegree.NavEmployee().EmployeeId     // outer data source is being used within CTE part here
                                                        select subOrdinate
                                                        )
                                                        .RecursiveUnion(anchorSource => from manager in employees
                                                                                        join subOrdinate in anchorSource on manager.ManagerId equals subOrdinate.ManagerId
                                                                                        select manager)
                                                        .Where(manager => manager.Name.StartsWith("A"))
                                                        .Select(manager => manager.ManagerId)
                                                        .FirstOrDefault()
                                        ,
                        
                    };
            string? expectedResult = null;
            Test("Recursive Union With Sub Query Outer Data Source In Anchor Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00496_RecursiveUnionWithSubQueryOuterDataSourceInRecursiveTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from employeeDegree in employeeDegrees
                    where employeeDegree.EmployeeId == "123"
                    select new
                    {
                        employeeDegree.EmployeeId,
                        employeeDegree.NavEmployee().Name,
                        FirstManagerStartsWithA = (
                                                        from manager in employees
                                                        select manager
                                                        )
                                                        .RecursiveUnion(anchorSource => from subOrdinate in employees
                                                                                        join manager in anchorSource on subOrdinate.ManagerId equals manager.EmployeeId
                                                                                        where subOrdinate.EmployeeId == employeeDegree.NavEmployee().EmployeeId
                                                                                        select manager)                                                    
                                                        .Select(manager => manager.ManagerId)
                                                        .FirstOrDefault()
                                        ,

                    };
            string? expectedResult = null;
            Test("Recursive Union With Sub Query Outer Data Source In Recursive Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00497_RecursiveUnionInSubQueryTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from manager in employees
                    where manager.EmployeeId == "123"       // picking specific manager
                    select new
                    {
                        ManagerId = manager.EmployeeId,
                        ManagerName = manager.Name,
                        NestedCount = employees        // anchor
                                            .Where(immediateChild => immediateChild.ManagerId == manager.EmployeeId)
                                            .RecursiveUnion(anchor => anchor.SelectMany(anchorMember => anchorMember.NavSubOrdinates))
                                            .Count()
                    };


            string? expectedResult = null;
            Test("Recursive Union In Sub Query Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00498_RecursiveUnionWithSubQueryJoinTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from manager in employees
                    where manager.EmployeeId == "123"       // picking specific manager
                    select new
                    {
                        ManagerId = manager.EmployeeId,
                        ManagerName = manager.Name,
                        FilteredNested = manager.NavNestedChildren.Where(x => x.NavEmployee().Department == "IT").Count(),
                    };


            string? expectedResult = null;
            Test("Recursive Union With Sub Query Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00500_ToMultipleChildrenNavigationPropertyTest()
        {
            IQueryable<StudentGrade> studentGrades = new Queryable<StudentGrade>(new QueryProvider());
            var q = studentGrades.Where(x => x.NavStudentGradeDetails.Any(y => y.MarksGained > 50));
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.StudentId as StudentId, a_1.Grade as Grade
from	StudentGrade as a_1
where	exists(
	select	1
	from	StudentGradeDetail as a_2
	where	(a_1.RowId = a_2.StudentGradeRowId)
		and	(a_2.MarksGained > 50)
)
";
            Test("To Multiple Children Navigation Property Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00510_ToMultipleChildrenWithToOneNavigationPropertyTest()
        {
            IQueryable<ItemBase> items = new Queryable<ItemBase>(new QueryProvider());
            var q = items.Where(x => x.NavItemExt().NavParts.Any(y => y.PartNumber == "123"));
            string? expectedResult = $@"
	select	a_1.ItemId as ItemId, a_1.ItemDescription as ItemDescription
	from	ItemBase as a_1
		left join ItemExtension as NavItemExt_2 on (a_1.ItemId = NavItemExt_2.ItemId)
	where	exists(
		select	1
		from	ItemPart as a_3
		where	(NavItemExt_2.ItemId = a_3.ItemId) and (a_3.PartNumber = '123')
	)
";
            Test("To Multiple Children With To One Navigation Property Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00520_RecursiveWithNoSelectionTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                        .Where(x => x.ManagerId == null)
                        .RecursiveUnion(
                            anchorSource => employees.InnerJoin(
                                                            anchorSource,
                                                            (recursiveMember, anchorMember) => new { anchor = anchorMember, recursive = recursiveMember },
                                                            newShape => newShape.anchor.EmployeeId == newShape.recursive.ManagerId
                                                        )
                                                        .Select(newShape => newShape.recursive))
                        ;
            string? expectedResult = @"
with cte_1 as 
(	
	select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Name as Name, a_2.Department as Department, a_2.ManagerId as ManagerId	
	from	Employee as a_2	
	where	a_2.ManagerId is null	
	union all	
	select	a_3.RowId as RowId, a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.Department as Department, a_3.ManagerId as ManagerId	
	from	Employee as a_3	
		inner join cte_1 as a_4 on (a_4.EmployeeId = a_3.ManagerId)	
)
select	cte_1.RowId as RowId, cte_1.EmployeeId as EmployeeId, cte_1.Name as Name, cte_1.Department as Department, cte_1.ManagerId as ManagerId
from	cte_1 as cte_1
";
            Test("Recursive With No Selection Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00530_QuerySelectManyTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = employees
                    .SelectMany(e => employeeDegrees)
                    ;
            string? expectedResult = $@"
select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		cross join EmployeeDegree as a_2
";
            Test("Query Select Many Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00540_QuerySelectManyWithWhereAndSelectTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = employees
                    .SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Select(x => new { x.EmployeeId, x.Degree }))
                    ;
            string? expectedResult = $@"
select	a_3.EmployeeId as EmployeeId, a_3.Degree as Degree
	from	Employee as a_1
		cross apply (
			select	a_2.EmployeeId as EmployeeId, a_2.Degree as Degree
			from	EmployeeDegree as a_2
			where	(a_2.EmployeeId = a_1.EmployeeId)
		) as a_3
";
            Test("Query Select Many With Where and Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00550_QuerySelectManyWithNavigationTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                    .SelectMany(e => e.NavDegrees)
                    ;
            string? expectedResult = @"
select	NavDegrees_2.RowId as RowId, NavDegrees_2.EmployeeId as EmployeeId, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
    	from	Employee as a_1
    		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
";
            Test("Query Select Many With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00560_QuerySelectManyWithNavigationComplexTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                    .SelectMany(e => e.NavDegrees)
                    .Where(x => x.Degree == "!23")
                    .OrderBy(x => x.University)
                    .Select(x => new { x.Degree, x.University })
                    ;
            string? expectedResult = @"
select	NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
	where	(NavDegrees_2.Degree = '!23')
	order by NavDegrees_2.University asc
";
            Test("Query Select Many With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00570_SelectManyWithNavigationAndSelectTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                    .SelectMany(e => e.NavDegrees, (e, ed) => new { e.EmployeeId, e.Name, ed.Degree, ed.University })
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
    	from	Employee as a_1
    		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
";
            Test("Query Select Many With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00580_SelectManyWithSelectTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());

            var q = employees
                    .SelectMany(e => employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId), (e, ed) => new { e.EmployeeId, e.Name, ed.Degree, ed.University })
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, child_join_2.Degree as Degree, child_join_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as child_join_2 on (child_join_2.EmployeeId = a_1.EmployeeId)
";
            Test("Query Select Many With Select Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00590_SelectMultiLevelWithSubQueryTest()
        {
            var queryProvider = new QueryProvider();
            var q = QueryExtensions
                            .From(queryProvider, () => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                            .LeftJoin(x => x.ed, x => x.e.EmployeeId == x.ed.EmployeeId)
                            .LeftJoin(QueryExtensions.DataSet<Employee>(queryProvider), (o, j) => new { o, m = j }, n => n.o.e.ManagerId == n.m.EmployeeId)
                            .Select(x => new { FullDetail = new { Employee = x.o.e, Name = x.o.e.Name }, x.o.ed.RowId, x.m })
                            .Select(x => new { x.FullDetail.Employee.RowId, x.FullDetail.Employee.Name, EmployeeDegreeRowId = x.RowId, ManagerName = x.m.Name })

                    ;
            string? expectedResult = @"
select	a_4.RowId as RowId, a_4.Name as Name, a_4.RowId_1 as EmployeeDegreeRowId, a_4.Name_2 as ManagerName
	from	(
		select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_1.Name as Name_1, a_2.RowId as RowId_1, a_3.RowId as RowId_2, a_3.EmployeeId as EmployeeId_1, a_3.Name as Name_2, a_3.Department as Department_1, a_3.ManagerId as ManagerId_1
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
			left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
	) as a_4
";
            Test("Select Multi Level With Sub Query Test", q.Expression, expectedResult);
        }

        private class MultiLevelResult
        {
            public Employee Employee { get; set; }
            public EmployeeDegree EmployeeDegree { get; set; }
        }

        [TestMethod]
        public void T00600_SelectMultiLevelWithMemberInitExpressionTest()
        {
            var queryProvider = new QueryProvider();
            var q = QueryExtensions
                            .From(queryProvider, () => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                            .LeftJoin(x => x.ed, x => x.e.EmployeeId == x.ed.EmployeeId)
                            .LeftJoin(QueryExtensions.DataSet<Employee>(queryProvider), (o, j) => new { o, m = j }, n => n.o.e.ManagerId == n.m.EmployeeId)
                            .Select(x => new MultiLevelResult { Employee = x.o.e, EmployeeDegree = x.o.ed })
                            .Select(x => new { EmployeeRowId = x.Employee.RowId, DegreeRowId = x.EmployeeDegree.RowId, x.Employee.Name })
                            ;
            string? expectedResult = @"
select	a_4.RowId as EmployeeRowId, a_4.RowId_1 as DegreeRowId, a_4.Name as Name
	from	(
		select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
			left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
	) as a_4
";
            Test("Select Multi Level With Member Init Expression Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00610_NestedFromTest()
        {
            var queryProvider = new QueryProvider();
            var q = QueryExtensions
                            .From(queryProvider, () => new
                            {
                                q1 = QueryExtensions.From(queryProvider, () => new { e = QueryExtensions.Table<Employee>(), ed = QueryExtensions.Table<EmployeeDegree>() })
                                                            .LeftJoin(f1 => f1.ed, fj1 => fj1.e.EmployeeId == fj1.ed.EmployeeId)
                                                            .Schema(),
                                m = QueryExtensions.Table<Employee>()
                            })
                            .LeftJoin(f2 => f2.m, fj2 => fj2.q1.e.ManagerId == fj2.m.EmployeeId)
                            .LeftJoin(new Queryable<Employee>(queryProvider), (o, j3) => new { o, m2 = j3 }, n => n.o.q1.e.ManagerId == n.m2.EmployeeId)
                            .Select(x => new { EmRowId = x.o.q1.e.RowId, EdRowId = x.o.q1.ed.RowId, M1RowId = x.o.m.RowId, M2RowId = x.m2.RowId })
                            ;
            string? expectedResult = @"
select	a_3.RowId as EmRowId, a_3.RowId_1 as EdRowId, a_4.RowId as M1RowId, a_5.RowId as M2RowId
	from	(
		select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
	) as a_3
		left join Employee as a_4 on (a_3.ManagerId = a_4.EmployeeId)
		left join Employee as a_5 on (a_3.ManagerId = a_5.EmployeeId)
";
            Test("Nested From Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00620_NestedSelectWithMemberTest()
        {
            var provider = new QueryProvider();
            var employees = QueryExtensions.DataSet<Employee>(provider);
            var employeeDegrees = QueryExtensions.DataSet<EmployeeDegree>(provider);
            var q = employees
                .LeftJoin(employeeDegrees, (e, ed) => new { e, ed }, j => j.e.EmployeeId == j.ed.EmployeeId)
                .Select(x => new { x.e.EmployeeId, x.e.Name, x.ed.Degree })
                .Select(x => new { x.EmployeeId, x.Name })
                ;
            string? expectedResult = @"
select	a_3.EmployeeId as EmployeeId, a_3.Name as Name
	from	(
		select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree
		from	Employee as a_1
			left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
	) as a_3
";
            Test("Nested Select With Member Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00640_QuerySyntaxNavigationTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    from ed in e.NavDegrees
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
";
            Test("Query Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00650_QuerySyntaxStandardJoinMultipleFieldsTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    join ed in employeeDegrees on new { e.RowId, e.EmployeeId } equals new { ed.RowId, ed.EmployeeId }
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as a_2 on ((a_1.RowId = a_2.RowId) and (a_1.EmployeeId = a_2.EmployeeId))
";
            Test("Standard Join Multiple Fields Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void T00651_QuerySyntaxStandardJoinLeftTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    join ed in employeeDegrees on new { e.RowId, e.EmployeeId } equals new { ed.RowId, ed.EmployeeId }
                    into eds from ed2 in eds.DefaultIfEmpty()
                    join m in employees on e.ManagerId equals m.EmployeeId
                    into ms from m2 in ms.DefaultIfEmpty()
                    select new { e.EmployeeId, e.Name, ed2.Degree, ed2.University, ManagerName = m2.Name }
                    ;

            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University, a_3.Name as ManagerName
	from	Employee as a_1
		left join EmployeeDegree as a_2 on ((a_1.RowId = a_2.RowId) and (a_1.EmployeeId = a_2.EmployeeId))
		left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
";
            Test("Standard Join Left Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void T00660_QuerySyntaxStandardJoinTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    join ed in employeeDegrees on e.EmployeeId equals ed.EmployeeId
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
";
            Test("Standard Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00670_QuerySyntax3DataSourcesTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            IQueryable<Employee> managers = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    from ed in employeeDegrees
                    from m in managers
                    from m2 in managers
                    from m3 in managers
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University, ManagerName = m.Name, Manager2Name = m2.Name, Manager3Name = m3.Name }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_2.Degree as Degree, a_2.University as University, a_3.Name as ManagerName, a_4.Name as Manager2Name, a_5.Name as Manager3Name
	from	Employee as a_1
		cross join EmployeeDegree as a_2
		cross join Employee as a_3
		cross join Employee as a_4
        cross join Employee as a_5
";
            Test("3 Data Sources Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00671_QuerySyntaxMultiFromNavigationTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    from ed in e.NavDegrees
                    from ms in ed.NavMarksheets
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University, Course = ms.Course, Grade = ms.Grade }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University, NavMarksheets_3.Course as Course, NavMarksheets_3.Grade as Grade
	from	Employee as a_1
		inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
		inner join Marksheet as NavMarksheets_3 on (NavDegrees_2.RowId = NavMarksheets_3.EmployeeDegreeRowId)
";
            Test("Query Syntax Multiple From Navigation Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void T00672_QuerySyntaxFromWithWhereConvertedToJoinTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId)
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, child_join_2.Degree as Degree, child_join_2.University as University
	from	Employee as a_1
		inner join EmployeeDegree as child_join_2 on (child_join_2.EmployeeId = a_1.EmployeeId)
";
            Test("Query Syntax From with Where Converted to Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00673_QuerySyntaxFromWithWhereButTakeAtEndConvertToCrossApplyTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<EmployeeDegree> employeeDegrees = new Queryable<EmployeeDegree>(new QueryProvider());
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.EmployeeId == e.EmployeeId).Take(5)
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		cross apply (
			select	top (5)	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
			from	EmployeeDegree as a_2
			where	(a_2.EmployeeId = a_1.EmployeeId)
		) as a_3
";
            Test("Query Syntax From with Where But 'Take' at End Convert to Cross Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00674_QuerySyntaxMultiFromNavigationDefaultIfEmptyTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    from ed in e.NavDegrees.DefaultIfEmpty()
                    from ms in ed.NavMarksheets
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University, Course = ms.Course, Grade = ms.Grade }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavDegrees_2.University as University, NavMarksheets_3.Course as Course, NavMarksheets_3.Grade as Grade
	from	Employee as a_1
		left join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
		inner join Marksheet as NavMarksheets_3 on (NavDegrees_2.RowId = NavMarksheets_3.EmployeeDegreeRowId)
";
            Test("Query Syntax Multi From Navigation one as Default If Empty Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00675_QuerySyntaxMultiFromSubQueryToCrossJoinTest()
        {
            var employees = QueryExtensions.DataSet<Employee>(dbc);
            var employeeDegrees = QueryExtensions.DataSet<EmployeeDegree>(dbc);
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.Degree == "123")
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		cross join (
			select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
			from	EmployeeDegree as a_2
			where	(a_2.Degree = '123')
		) as a_3
";
            Test("Query Syntax Multi From Sub Query to Cross Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00676_QuerySyntaxMultiFromSubQueryToCrossApplyTest()
        {
            var employees = QueryExtensions.DataSet<Employee>(dbc);
            var employeeDegrees = QueryExtensions.DataSet<EmployeeDegree>(dbc);
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.Degree == "123").Select(x => new { x.Degree, x.University, e.Department })
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		cross apply (
			select	a_2.Degree as Degree, a_2.University as University, a_1.Department as Department
			from	EmployeeDegree as a_2
			where	(a_2.Degree = '123')
		) as a_3
";
            Test("Query Syntax Multi From Sub Query to Cross Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00677_QuerySyntaxMultiFromSubQueryToCrossApplyDefaultIfEmptyTest()
        {
            var employees = QueryExtensions.DataSet<Employee>(dbc);
            var employeeDegrees = QueryExtensions.DataSet<EmployeeDegree>(dbc);
            var q = from e in employees
                    from ed in employeeDegrees.Where(x => x.Degree == "123").Select(x => new { x.Degree, x.University, e.Department }).DefaultIfEmpty()
                    select new { e.EmployeeId, e.Name, ed.Degree, ed.University }
                    ;
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_3.Degree as Degree, a_3.University as University
	from	Employee as a_1
		outer apply (
			select	a_2.Degree as Degree, a_2.University as University, a_1.Department as Department
			from	EmployeeDegree as a_2
			where	(a_2.Degree = '123')
		) as a_3
";
            Test("Query Syntax Multi From Sub Query to Cross Apply Default If Empty Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00678_QuerySyntaxSelectMultipleTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q = from e in employees
                    select new { e.EmployeeId, e.Name }
                    into r1
                    select new { Col1 = r1.EmployeeId, Col2 = r1.Name }
                    into r2
                    select new { r2.Col1, r2.Col2 }
                    ;
            string? expectedResult = @"
select	a_3.Col1 as Col1, a_3.Col2 as Col2
	from	(
		select	a_2.EmployeeId as Col1, a_2.Name as Col2
		from	(
			select	a_1.EmployeeId as EmployeeId, a_1.Name as Name
			from	Employee as a_1
		) as a_2
	) as a_3
";
            Test("Query Syntax Select Multiple Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00680_QuerySyntaxMultipleDataSourcesWithNavigationTest()
        {
            IQueryable<Employee> employees = new Queryable<Employee>(new QueryProvider());
            var q =
                    from e in employees
                    from ed in e.NavDegrees
                    from subOrdinate in e.NavSubOrdinates
                    from subOrdinateDegree in subOrdinate.NavDegrees
                    select new { e.EmployeeId, e.Name, ed.Degree, SubOrdinate = new { n1 = new { n1 = subOrdinate }, f2 = subOrdinate.EmployeeId }, SubOrdinateDegree = subOrdinateDegree.Degree }
                    into final
                    select new { z = final.SubOrdinate.n1 }
                    into next
                    select new { y = next.z.n1.ManagerId }
                    ;
            string? expectedResult = @"
select	a_6.ManagerId as y
	from	(
		select	a_5.RowId as RowId, a_5.EmployeeId_1 as EmployeeId_1, a_5.Name_1 as Name_1, a_5.Department as Department, a_5.ManagerId as ManagerId
		from	(
			select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, NavDegrees_2.Degree as Degree, NavSubOrdinates_3.RowId as RowId, NavSubOrdinates_3.EmployeeId as EmployeeId_1, NavSubOrdinates_3.Name as Name_1, NavSubOrdinates_3.Department as Department, NavSubOrdinates_3.ManagerId as ManagerId, NavSubOrdinates_3.EmployeeId as f2, NavDegrees_4.Degree as SubOrdinateDegree
			from	Employee as a_1
				inner join EmployeeDegree as NavDegrees_2 on (a_1.EmployeeId = NavDegrees_2.EmployeeId)
				inner join Employee as NavSubOrdinates_3 on (a_1.EmployeeId = NavSubOrdinates_3.ManagerId)
				inner join EmployeeDegree as NavDegrees_4 on (NavSubOrdinates_3.EmployeeId = NavDegrees_4.EmployeeId)
		) as a_5
	) as a_6
";
            Test("Query Syntax with Multiple Data Sources having Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00690_MultiDataSourceSelectionNewExpressionTest()
        {
            var q = QueryExtensions.DataSet<Employee>(dbc)
                    .LeftJoin(QueryExtensions.DataSet<EmployeeDegree>(dbc), (e, ed) => new { e, ed }, j => j.e.EmployeeId == j.ed.EmployeeId)
                    .LeftJoin(QueryExtensions.DataSet<Employee>(dbc), (o, m) => new { o, m }, n => n.o.e.ManagerId == n.m.EmployeeId)
                    .Select(x => new { t = x.o })
                    ;
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
		left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
";
            Test("Multi Data Source Selection Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00700_MultiDataSourceSelectionDirectSelectionTest()
        {
            var q = QueryExtensions.DataSet<Employee>(dbc)
                    .LeftJoin(QueryExtensions.DataSet<EmployeeDegree>(dbc), (e, ed) => new { e, ed }, j => j.e.EmployeeId == j.ed.EmployeeId)
                    .LeftJoin(QueryExtensions.DataSet<Employee>(dbc), (o, m) => new { o, m }, n => n.o.e.ManagerId == n.m.EmployeeId)
                    .Select(x => x.o)
                    ;
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId, a_2.RowId as RowId_1, a_2.EmployeeId as EmployeeId_1, a_2.Degree as Degree, a_2.University as University
	from	Employee as a_1
		left join EmployeeDegree as a_2 on (a_1.EmployeeId = a_2.EmployeeId)
		left join Employee as a_3 on (a_1.ManagerId = a_3.EmployeeId)
";
            Test("Multi Data Source Selection Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00710_NavChildrenSubQuerySimpleTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.Select(x => new
            {
                x.EmployeeId,
                x.Name,
                DegreeCount = x.NavDegrees.Count()
            });
            string? expectedResult = @"
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
        public void T00720_NavChildrenSubQueryComplexTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.Select(x => new
            {
                x.EmployeeId,
                x.Name,
                DegreeCount = x.NavDegrees.GroupBy(y => y.EmployeeId).Select(y => new { E_ID = y.Key, MaxDeg = y.Max(z => z.Degree) }).Count()
            });
            string? expectedResult = @"
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
        public void T00730_NavChildrenSubQueryTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.Select(x => new
            {
                x.EmployeeId,
                x.Name,
                DegreeCount = x.NavDegrees.SelectMany(y => y.NavMarksheets).Count()
            });
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, (
		select	Count(1) as Col1
		from	(
			select	a_2.RowId as RowId, a_2.EmployeeId as EmployeeId, a_2.Degree as Degree, a_2.University as University
			from	EmployeeDegree as a_2
			where	(a_1.EmployeeId = a_2.EmployeeId)
		) as a_3
			inner join Marksheet as NavMarksheets_4 on (a_3.RowId = NavMarksheets_4.EmployeeDegreeRowId)
	) as DegreeCount
	from	Employee as a_1
";
            Test("Nav Children Sub Query Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void T00740_NavChildrenSubQueryWhereTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees
                .Where(x => x.NavDegrees.Where(y => y.NavMarksheets.Any(z => z.TotalMarks > 50)).Any())
                .Select(x => new
                {
                    x.EmployeeId,
                    x.Name,
                });
            string? expectedResult = @"
select	a_1.EmployeeId as EmployeeId, a_1.Name as Name
	from	Employee as a_1
	where	exists(
		select	1
		from	EmployeeDegree as a_2
		where	(a_1.EmployeeId = a_2.EmployeeId)
			and	exists(
			select	1
			from	Marksheet as a_3
			where	(a_2.RowId = a_3.EmployeeDegreeRowId)
				and	(a_3.TotalMarks > 50)
		)
	)
";
            Test("Nav Children Sub Query Where Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00750_GroupByHavingTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            var q = employees.GroupBy(x => new { x.ManagerId, x.Department })
                                .Having(x => x.Count() > 1)
                                .Select(b => new { b.Key.ManagerId, b.Key.Department, TotalLines = b.Count(), MaxV = b.Max(y => y.EmployeeId) })
                                .Select(c => c.MaxV)
                                ;
            string? expectedResult = @"
select	a_2.MaxV as Col1
	from	(
		select	a_1.ManagerId as ManagerId, a_1.Department as Department, Count(1) as TotalLines, Max(a_1.EmployeeId) as MaxV
		from	Employee as a_1
		group by a_1.ManagerId, a_1.Department
		having	(Count(1) > 1)
	) as a_2
";
            Test("Group By Having Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00760_ComplexGroupByTest()
        {
            var d = new DateTime(2020, 1, 1);
            var students = new Queryable<Student>(new QueryProvider());
            var q = students
                        .Where(x => x.StudentId == "123")
                        .Where(x => x.RecordCreateDate > d)
                        .GroupBy(x => new { x.CountryID, x.StudentType })
                        .Having(x => x.Count() > 1)
                        .Select(x => new { x.Key.CountryID, SType = x.Key.StudentType, MaxAdmDate = x.Max(y => y.AdmissionDate) })
                        .OrderBy(x => x.CountryID)
                        .Where(x => x.SType == "345")
                        .Select(x => x.CountryID)
                        ;
            string? expectedResult = @"
select	a_2.CountryID as Col1
	from	(
		select	a_1.CountryID as CountryID, a_1.StudentType as SType, Max(a_1.AdmissionDate) as MaxAdmDate
		from	Student as a_1
		where	(a_1.StudentId = '123')
			and	(a_1.RecordCreateDate > '2020-01-01 00:00:00')
		group by a_1.CountryID, a_1.StudentType
		having	(Count(1) > 1)
		order by CountryID asc
	) as a_2
	where	(a_2.SType = '345')
";
            Test("Complex Group By Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00770_WhereBooleanCoalesceConditionTest()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => x.HasScholarship ?? false).Select(x => new { x.StudentId, x.Name });
            string? expectedResult = @"
select	a_1.StudentId as StudentId, a_1.Name as Name
	from	Student as a_1
	where	isnull(a_1.HasScholarship, False)";
            Test("Where Boolean Coalesce Condition Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00770_WhereOrTest()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => x.Name.StartsWith("555"));
            q = q.Where(x => 1 == 2);
            q = q.WhereOr(x => x.CountryID == "1");
            q = q.WhereOr(x => x.CountryID == "2");
            q = q.Where(x => x.HasScholarship == true);
            string? expectedResult = @"select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Name like ('555' + '%')) and (False or (a_1.CountryID = '1') or (a_1.CountryID = '2')) and (a_1.HasScholarship = True)";
            Test("Where OR Condition Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00780_FullOuterJoinTest()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var studentGrades = new Queryable<StudentGrade>(new QueryProvider());
            var q = students.FullOuterJoin(studentGrades, (x, y) => new { x, y }, (r) => r.x.StudentId == r.y.StudentId)
                            .Select(f => new { StudentTableStudentId = f.x.StudentId, StudentGradeTableStudentId = f.y.StudentId });
            string? expectedResult = @"select	a_1.StudentId as StudentTableStudentId, a_2.StudentId as StudentGradeTableStudentId
	from	Student as a_1
		full outer join StudentGrade as a_2 on (a_1.StudentId = a_2.StudentId)";
            Test("Full Outer Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00790_CalculatedPropertyTest()
        {
            var marksheets = new Queryable<Marksheet>(new QueryProvider());

            var q = marksheets.Where(x => x.CalcPercentage > 50).Select(x => new { x.Course, x.Grade });

            string? expectedResult = @"
    select	a_1.Course as Course, a_1.Grade as Grade
	from	Marksheet as a_1
	where	(case when (a_1.TotalMarks > 0) then ((a_1.MarksGained / a_1.TotalMarks) * 100.0) else 0 end > 50)
";
            Test("Calculated Property Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00800_CalculatedPropertyWithNavigationTest()
        {
            var invoices = new Queryable<Invoice>(new QueryProvider());
            var q = invoices.Where(x => x.CalcInvoiceTotal > 100).Select(x => new { x.InvoiceId, x.InvoiceDate });
            string? expectedResult = @"
select	a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate
	from	Invoice as a_1
	where	((
		select	Sum(a_2.LineTotal) as Col1
		from	InvoiceDetail as a_2
		where	(a_1.RowId = a_2.InvoiceId)
	) > 100)
";
            Test("Calculated Property With Navigation Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00810_SpecificationPreInitializedTest()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var specification = new StudentIsAdultSpecification();
            var q = students.Where(x => specification.IsSatisfiedBy(x));
            string? expectedResult = @"select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Age >= 18)";
            Test("Specification Pre Initialized Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00820_SpecificationInlineTest()
        {
            var students = new Queryable<Student>(new QueryProvider());
            var q = students.Where(x => new StudentIsAdultSpecification().IsSatisfiedBy(x));
            string? expectedResult = @"select	a_1.StudentId as StudentId, a_1.Name as Name, a_1.Address as Address, a_1.Age as Age, a_1.AdmissionDate as AdmissionDate, a_1.RecordCreateDate as RecordCreateDate, a_1.RecordUpdateDate as RecordUpdateDate, a_1.StudentType as StudentType, a_1.CountryID as CountryID, a_1.HasScholarship as HasScholarship
	from	Student as a_1
	where	(a_1.Age >= 18)";
            Test("Specification Inline Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00830_SpecificationWithConstructorArgumentsTest()
        {
            var invoices = new Queryable<Invoice>(new QueryProvider());

            var q = invoices.Where(x => new InvoiceIsDueOnGivenDateSpecification(x.InvoiceDate).IsSatisfiedBy(x))
                              .Where(x => !new CustomerIsInvalidSpecification().IsSatisfiedBy(x.NavCustomer()));

            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.InvoiceId as InvoiceId, a_1.InvoiceDate as InvoiceDate, a_1.Description as Description, a_1.CustomerId as CustomerId, a_1.DueDate as DueDate
	from	Invoice as a_1
		inner join Customer as NavCustomer_2 on (NavCustomer_2.RowId = a_1.CustomerId)
	where	(a_1.DueDate >= a_1.InvoiceDate) and not ((NavCustomer_2.Status = 'Disabled') or (NavCustomer_2.Status = 'Blocked'))
";
            Test("Specification With Constructor Arguments Test", q.Expression, expectedResult);
        }


        [TestMethod]
        public void T00840_NavigationToChildPropertyToOuterApplyTest()
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

            string? expectedResult = @"
select	a_1.InvoiceId as InvoiceId, NavFirstLine_3.ItemId as Item, NavItem_4.ItemDescription as ItemDescription, NavFirstLine_3.UnitPrice as UnitPrice
	from	Invoice as a_1
		outer apply (
			select	top (1)	a_2.RowId as RowId, a_2.InvoiceId as InvoiceId, a_2.ItemId as ItemId, a_2.UnitPrice as UnitPrice, a_2.Quantity as Quantity, a_2.LineTotal as LineTotal
			from	InvoiceDetail as a_2
			where	(a_1.RowId = a_2.InvoiceId)
		) as NavFirstLine_3
		left join ItemBase as NavItem_4 on (NavItem_4.ItemId = NavFirstLine_3.ItemId)
";

            Test("Calculated Property to Outer Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00850_NavigationToChildWithMoreThan1LinesOuterApplyTest()
        {
            var queryProvider = new QueryProvider();
            var invoices = new Queryable<Invoice>(queryProvider);
            var invoiceDetails = new Queryable<InvoiceDetail>(queryProvider);
            var q = invoices.Select(x => new { x.InvoiceId, x.NavTop2Lines().ItemId, x.NavTop2Lines().UnitPrice });
            string? expectedResult = @"
select	a_1.InvoiceId as InvoiceId, NavTop2Lines_3.ItemId as ItemId, NavTop2Lines_3.UnitPrice as UnitPrice
	from	Invoice as a_1
		outer apply (
			select	top (2)	a_2.RowId as RowId, a_2.InvoiceId as InvoiceId, a_2.ItemId as ItemId, a_2.UnitPrice as UnitPrice, a_2.Quantity as Quantity, a_2.LineTotal as LineTotal
			from	InvoiceDetail as a_2
			where	(a_1.RowId = a_2.InvoiceId)
		) as NavTop2Lines_3
";
            Test("Navigation to Child with More than 1 Lines Outer Apply Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00860_CalculatedPropertyInInterfaceTest()
        {
            var employees = new Queryable<Employee>(new QueryProvider());
            IQueryable<IFullName> q1 = employees;
            var q = q1.Where(x => x.CalcFullName.Contains("Abc"));
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.Department as Department, a_1.ManagerId as ManagerId
	from	Employee as a_1
	where	(a_1.Name like ('%' + ('Abc' + '%')))
";

            Test("Calculated Property in Interface Test", q.Expression, expectedResult);
        }



        [TestMethod]
        public void T00861_NavigationFullSelectionTest()
        {
            var assets = new Queryable<Asset>(new QueryProvider());
            var q = assets.Where(x => x.NavItem().ItemId == "333" || x.NavItem().ItemId == "111").Select(x => x).Select(x => new { x.NavItem().ItemId, x.NavItem().ItemDescription });
            string? expectedResult = @"
select	NavItem_4.ItemId as ItemId, NavItem_4.ItemDescription as ItemDescription
	from	(
		select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
		from	Asset as a_1
			inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
		where	((NavItem_2.ItemId = '333') or (NavItem_2.ItemId = '111'))
	) as a_3
		inner join ItemBase as NavItem_4 on (NavItem_4.ItemId = a_3.ItemId)
";
            Test("Navigation Full Selection Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00870_CastingWithinQueryTest()
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
                    string? expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
	from	Asset as a_1
		inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	where	(NavItem_2.ItemDescription like ('%' + ('Abc' + '%')))
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
                    string? expectedResult = @"
select	a_1.EquipId as EquipId, a_1.Model as Model, a_1.ItemId as ItemId
	from	Equipment as a_1
	where	exists(
		select	1
		from	Asset as a_2
			inner join ItemBase as NavItem_3 on (NavItem_3.ItemId = a_2.ItemId)
		where	(a_1.ItemId = NavItem_3.ItemId) and (NavItem_3.ItemDescription like ('%' + ('Abc' + '%')))
	)
";
                    Test("Casting Within Query Test-02", q.Expression, expectedResult);
                }
            }

            void internalMethod3<T>(IQueryable<T> query) where T : IModelWithItem
            {
                var q = query.Where(x => x.NavItem().ItemDescription.Contains("Abc"));
                string? expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
	from	Asset as a_1
		inner join ItemBase as NavItem_2 on (NavItem_2.ItemId = a_1.ItemId)
	where	(NavItem_2.ItemDescription like ('%' + ('Abc' + '%')))
";
                Test("Casting Within Query Test-03", q.Expression, expectedResult);
            }

            void internalMethod4<T>(IQueryable<T> query) where T : class
            {
                if (query is IQueryable<IModelWithItem> queryWithItem)
                {
                    var q = queryWithItem.Where(x => x.NavItem().ItemId == "333").Select(x => x).Select(x => new { x.NavItem().ItemId, x.NavItem().ItemDescription });
                    string? expectedResult = @"
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
        public void T00880_JoinAfterWhereTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);
            var q = assets.Where(x => x.ItemId == "123")
                .LeftJoin(items, (a, i) => new { a, i }, (j) => j.a.ItemId == j.i.ItemId)
                .Select(x=> new { x.a.ItemId, x.a.SerialNumber, x.i.ItemDescription, i2 = x.i.ItemId });
            ;
            string? expectedResult = @"
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
        public void T00890_StandardJoinAfterWhereTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);
            var q = from asset in assets
                    where asset.ItemId == "123"
                    join item in items on asset.ItemId equals item.ItemId into itemGroup
                    from item in itemGroup.DefaultIfEmpty()
                    select new { asset.ItemId, asset.SerialNumber, item.ItemDescription, i2 = item.ItemId };
            ;
            string? expectedResult = @"
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
        public void T00900_DistinctTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var q = assets.Select(x => new { x.ItemId, x.SerialNumber }).Distinct();
            string? expectedResult = @"
select  distinct a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
    from	Asset as a_1
";
            Test($"Distinct Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00901_MultipleDataSourceSelectedWithNormalJoinTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);

            var q = from asset in assets
                    join item in items on asset.ItemId equals item.ItemId
                    where asset.SerialNumber == "123"
                    select new { asset, item };

            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber, a_2.ItemId as ItemId_1, a_2.ItemDescription as ItemDescription
	from	Asset as a_1
		inner join ItemBase as a_2 on (a_1.ItemId = a_2.ItemId)
	where	(a_1.SerialNumber = '123')
";
            Test($"Multiple Data Source Selected With Normal Join Test", q.Expression, expectedResult);
        }

        [TestMethod]
        public void T00910_UpdateQuerySingleTableTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            Expression<Func<int>> expr = () => assets.Update(x => new Asset { SerialNumber = "ABC", Description = "Check" }, x => x.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
update a_1
	set SerialNumber = 'ABC',
		Description = 'Check'
from	Asset as a_1
where	(a_1.SerialNumber = '123')
";
            Test($"Update Query Single Table Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void T00911_UpdateQueryMultipleNavigationTableTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            Expression<Func<int>> expr = () => assets.Update(
                                                        asset => asset.NavItem(), 
                                                        asset => new ItemBase { ItemDescription = asset.NavItem().ItemDescription + asset.SerialNumber }, 
                                                        asset => asset.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
update NavItem_1
	set ItemDescription = (NavItem_1.ItemDescription + a_2.SerialNumber)
from	Asset as a_2
	inner join ItemBase as NavItem_1 on (NavItem_1.ItemId = a_2.ItemId)
where	(a_2.SerialNumber = '123')
";
            Test($"Update Query Multiple Table Navigation Test", queryExpression, expectedResult);
        }


        [TestMethod]
        public void T00911_UpdateQueryMultipleNormalJoinTableTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);

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

            var queryExpression = expr.Body;
            string? expectedResult = @"
update a_1
	set ItemDescription = (a_1.ItemDescription + a_2.SerialNumber)
from	Asset as a_2
	inner join ItemBase as a_1 on (a_2.ItemId = a_1.ItemId)
where	(a_2.SerialNumber = '123')
";
            Test($"Update Query Multiple Table Join Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void T00912_UpdateQueryMultipleTableFromWithNavigationTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);
            Expression<Func<int>> expr = () => dbc.From(() => new { asset = QueryExtensions.Table<Asset>(), item = QueryExtensions.Table<ItemBase>() })
                                                    .InnerJoin(x => x.item, x => x.asset.ItemId == x.item.ItemId)
                                                    .Select(x => x.item)
                                                    .Update(ms => ms.NavItemMoreInfo(), ms => new ItemMoreInfo { TrackingType = "TT" }, ms => ms.ItemDescription.Contains("123"));
            var queryExpression = expr.Body;
            string? expectedResult = @"
update NavItemMoreInfo_1
	set TrackingType = 'TT'
from	(
	select	a_3.ItemId as ItemId, a_3.ItemDescription as ItemDescription
	from	Asset as a_2
		inner join ItemBase as a_3 on (a_2.ItemId = a_3.ItemId)
) as a_4
	left join ItemMoreInfo as NavItemMoreInfo_1 on (a_4.ItemId = NavItemMoreInfo_1.ItemId)
where	(a_4.ItemDescription like ('%' + ('123' + '%')))
";
            Test($"Update Query Multiple Table From Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void T00913_UpdateQueryMultipleTableComplexObjectForModelPathTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            var items = new Queryable<ItemBase>(this.dbc);
            var moreInfo = new Queryable<ItemMoreInfo>(this.dbc);
            Expression<Func<int>> expr = () => assets
                                                    .InnerJoin(items, (assets, joinedTable) => new { a = assets, j1 = joinedTable }, newShape => newShape.a.ItemId == newShape.j1.ItemId)
                                                    .InnerJoin(moreInfo, (oldShape, joinedTable) => new { os1 = oldShape, j2 = joinedTable }, newShape => newShape.j2.ItemId == newShape.os1.j1.ItemId)
                                                    .Select(x => x.os1)
                                                    .Update(ms => ms.j1, ms => new ItemBase { ItemDescription = ms.a.SerialNumber + ms.j1.ItemId }, ms => ms.j1.ItemDescription.Contains("123"));
            var queryExpression = expr.Body;
            string? expectedResult = @"
  update a_1
	set ItemDescription = (a_2.SerialNumber + a_1.ItemId)
from	Asset as a_2
	inner join ItemBase as a_1 on (a_2.ItemId = a_1.ItemId)
	inner join ItemMoreInfo as a_3 on (a_3.ItemId = a_1.ItemId)
where	(a_1.ItemDescription like ('%' + ('123' + '%')))
";
            Test($"Update Query Multiple Table Complex Object For ModelPath Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void T00920_DeleteQuerySingleTableTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            Expression<Func<int>> expr = () => assets.Delete(x => x.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
delete a_1
from	Asset as a_1
where	(a_1.SerialNumber = '123')
";
            Test($"Delete Query Single Table Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void T00921_DeleteQueryMultipleNavigationTableTest()
        {
            var assets = new Queryable<Asset>(this.dbc);
            Expression<Func<int>> expr = () => assets.Delete(asset => asset.NavItem(),
                                                                asset => asset.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
delete NavItem_1
from	Asset as a_2
	inner join ItemBase as NavItem_1 on (NavItem_1.ItemId = a_2.ItemId)
where	(a_2.SerialNumber = '123')
";


            var employees = new Queryable<Employee>(this.dbc);

            var q = employees
                    .Select(x => new
                    {
                        C1 = x.EmployeeId,
                        SubordinateCount = x.NavSubOrdinates.Count()
                    });


            Test($"Delete Query Multiple Table Navigation Test", queryExpression, expectedResult);
        }


        [TestMethod]
        public void T01000_NestedConstantPropertyTest()
        {
            var p = new { p1 = new { p2 = new { v = "555" } } };
            var assets = new Queryable<Asset>(this.dbc);
            var q = assets.Where(x => x.ItemId == p.p1.p2.v);
            string? expectedResult = @"
select	a_1.RowId as RowId, a_1.Description as Description, a_1.ItemId as ItemId, a_1.SerialNumber as SerialNumber
	from	Asset as a_1
	where	(a_1.ItemId = '555')
";
            Test("Nested Constant Property Test", q.Expression, expectedResult);
        }
    }
}