using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.Preprocessors;
using Atis.SqlExpressionEngine.Services;
using Atis.SqlExpressionEngine.SqlExpressions;
using Atis.SqlExpressionEngine.UnitTest.Converters;

//using Atis.SqlExpressionEngine.UnitTest.Converters;
using Atis.SqlExpressionEngine.UnitTest.Preprocessors;
using Atis.SqlExpressionEngine.UnitTest.Services;

//using Atis.SqlExpressionEngine.UnitTest.Services;
using System.Linq.Expressions;
using Model = Atis.SqlExpressionEngine.UnitTest.Services.Model;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    public class DataContext
    {
        public IQueryable<Invoice> Invoices { get; } = new Queryable<Invoice>(new QueryProvider());
    }

    public abstract class TestBase
    {
        protected readonly IQueryProvider queryProvider = new QueryProvider();
        protected readonly DataContext dataContext = new();

        #region base methods
        protected void Test(string testHeading, Expression queryExpression, string expectedResult)
        {
            SqlExpression? result = null;
            Expression updatedQueryExpression = queryExpression;

            try
            {
                result = ConvertExpressionToSqlExpression(queryExpression, out updatedQueryExpression);
            }
            catch
            {
                printExpressions();
                throw;
            }

            string resultQuery = null;
            if (result != null)
            {
                var translator = new SqlExpressionTranslator()
                {
                    IsRowNumberSupported = false,
                };
                try
                {
                    resultQuery = translator.Translate(result);
                }
                catch
                {
                    printExpressions();
                    throw;
                }
                Console.WriteLine($"+++++++++++++++++++++++++ {testHeading} ++++++++++++++++++++++++");
                Console.WriteLine(resultQuery);
                Console.WriteLine("-----------------------------------------------------------------");
            }

            printExpressions();

            if (expectedResult != null && resultQuery != null)
            {
                ValidateQueryResults(resultQuery, expectedResult);
            }


            void printExpressions()
            {
                Console.WriteLine("Original Expression:");
                Console.WriteLine(ExpressionPrinter.PrintExpression(queryExpression));
                Console.WriteLine("Expression after Preprocessing:");
                Console.WriteLine(ExpressionPrinter.PrintExpression(updatedQueryExpression));
            }

        }

        private SqlExpression? ConvertExpressionToSqlExpression(Expression queryExpression, out Expression updatedQueryExpression)
        {
            var model = new Model();
            updatedQueryExpression = PreprocessExpression(queryExpression, model);
            var sqlDataTypeFactory = new SqlDataTypeFactory();
            var reflectionService = new ReflectionService(new ExpressionEvaluator());
            var parameterMapper = new LambdaParameterToDataSourceMapper();
            var sqlFactory = new SqlExpressionFactory();
            var logger = new Logger();
            var contextExtensions = new object[] { sqlDataTypeFactory, sqlFactory, model, parameterMapper, reflectionService, logger };
            var conversionContext = new ConversionContext(contextExtensions);
            var expressionConverterProvider = new LinqToSqlExpressionConverterProvider(conversionContext, factories: [new SqlFunctionConverterFactory(conversionContext)]);
            var postProcessorProvider = new SqlExpressionPostprocessorProvider(postprocessors: []);
            var linqToSqlConverter = new LinqToSqlConverter(reflectionService, expressionConverterProvider, postProcessorProvider);

            return linqToSqlConverter.Convert(updatedQueryExpression); // Let exception bubble up
        }


        protected Expression PreprocessExpression(Expression expression, IModel model)
        {
            //var stringLengthReplacementVisitor = new StringLengthReplacementVisitor();
            //expression = stringLengthReplacementVisitor.Visit(expression);
            var queryProvider = new QueryProvider();
            var reflectionService = new ReflectionService(new ExpressionEvaluator());
            var navigateToManyPreprocessor = new NavigateToManyPreprocessor(queryProvider, reflectionService);
            var navigateToOnePreprocessor = new NavigateToOnePreprocessor(reflectionService, queryProvider);
            var queryVariablePreprocessor = new QueryVariableReplacementPreprocessor();
            //var childJoinReplacementPreprocessor = new ChildJoinReplacementPreprocessor(reflectionService);
            var calculatedPropertyReplacementPreprocessor = new CalculatedPropertyPreprocessor(reflectionService);
            var specificationPreprocessor = new SpecificationCallRewriterPreprocessor(reflectionService);
            var convertPreprocessor = new ConvertExpressionReplacementPreprocessor();
            var allToAnyRewriterPreprocessor = new AllToAnyRewriterPreprocessor();
            var inValuesReplacementPreprocessor = new InValuesExpressionReplacementPreprocessor(reflectionService);
            //var nonPrimitivePropertyReplacementPreprocessor = new NonPrimitiveCalculatedPropertyPreprocessor(reflectionService);
            //var concreteParameterPreprocessor = new ConcreteParameterReplacementPreprocessor(new QueryPartsIdentifier(), reflectionService);
            var methodInterfaceTypeReplacementPreprocessor = new QueryMethodGenericTypeReplacementPreprocessor(reflectionService);
            var customMethodReplacementPreprocessor = new CustomBusinessMethodPreprocessor();
            var navigationEqualityPreprocessor = new NavigationNullEqualityPreprocessor(model);
            var preprocessor = new ExpressionPreprocessorProvider([queryVariablePreprocessor, methodInterfaceTypeReplacementPreprocessor, navigateToManyPreprocessor, navigateToOnePreprocessor, /*childJoinReplacementPreprocessor, */calculatedPropertyReplacementPreprocessor, specificationPreprocessor, convertPreprocessor, allToAnyRewriterPreprocessor, inValuesReplacementPreprocessor, customMethodReplacementPreprocessor,
                navigationEqualityPreprocessor
                /*, concreteParameterPreprocessor*/]);
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
            query = query.Replace("\r\n", "").Replace("\r", "").Replace("\n", " ").Replace("\t", "").Replace(" ", "").ToLower();
            //while (query.Contains("  "))
            //{
            //    query = query.Replace("  ", " ");
            //}
            return query;
        }
        #endregion
    }
}
