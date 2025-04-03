using Atis.Expressions;
using Atis.LinqToSql.Preprocessors;
using Atis.LinqToSql.Services;
using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{
    public abstract class TestBase
    {
        protected readonly IQueryProvider dbc = new QueryProvider();

        #region base methods
        protected void Test(string testHeading, Expression queryExpression, string? expectedResult)
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
            Console.WriteLine(ExpressionPrinter.PrintExpression(queryExpression));
            var updatedQueryExpression = PreprocessExpression(queryExpression);
            Console.WriteLine("Expression after Preprocessing:");
            Console.WriteLine(ExpressionPrinter.PrintExpression(updatedQueryExpression));

            //var componentIdentifier = new QueryComponentIdentifier();
            var model = new Model();
            var reflectionService = new ReflectionService(new ExpressionEvaluator());
            var parameterMapper = new LambdaParameterToDataSourceMapper();
            var sqlFactory = new SqlExpressionFactory();
            //var navigationMapper = new NavigationToDataSourceMapper();
            //var propertyMapper = new PropertyToDataSourceMapper();
            var contextExtensions = new object[] { sqlFactory, /*componentIdentifier,*/ model, parameterMapper, /*navigationMapper,*/ reflectionService/*, propertyMapper*/ };
            var conversionContext = new ConversionContext(contextExtensions);
            var expressionConverterProvider = new LinqToSqlExpressionConverterProvider(conversionContext, factories: null);
            var postProcessorProvider = new SqlExpressionPostprocessorProvider(sqlFactory, postprocessors: null);
            var linqToSqlConverter = new LinqToSqlConverter(reflectionService, expressionConverterProvider, postProcessorProvider);
            var result = linqToSqlConverter.Convert(updatedQueryExpression);
            return result;
        }

        protected Expression PreprocessExpression(Expression expression)
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
            var preprocessor = new ExpressionPreprocessorProvider([queryVariablePreprocessor, methodInterfaceTypeReplacementPreprocessor, navigateToManyPreprocessor, navigateToOnePreprocessor, childJoinReplacementPreprocessor, calculatedPropertyReplacementPreprocessor, specificationPreprocessor, convertPreprocessor/*, concreteParameterPreprocessor*/]);
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
    }
}
