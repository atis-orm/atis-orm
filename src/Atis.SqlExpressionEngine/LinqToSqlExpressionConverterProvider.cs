using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionConverters;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine
{
    public class LinqToSqlExpressionConverterProvider : ExpressionConverterProvider<Expression, SqlExpression>
    {
        public IConversionContext Context { get; }

        public LinqToSqlExpressionConverterProvider(IConversionContext context, IEnumerable<IExpressionConverterFactory<Expression, SqlExpression>> factories = null) : base(factories)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            this.Context = context;
            var defaultConverters = new IExpressionConverterFactory<Expression, SqlExpression>[]
            {
                new SchemaExpressionConverterFactory(context),
                new TableExpressionConverterFactory(context),
                new NullableValueExpressionConverterFactory(context),
                new NegateExpressionConverterFactory(context),  
                new NotExpressionConverterFactory(context),
                new CastExpressionConverterFactory(context),
                new StringLengthExpressionConverterFactory(context),
                //new ConstantQueryableExpressionConverterFactory(context),
                new ParameterExpressionConverterFactory(context),
                new GroupByKeyExpressionConverterFactory(context),
                new DateSubtractConverterFactory(context),
                new DateTimeMemberAccessConverterFactory(context),
                new VariableMemberExpressionConverterFactory(context),
                new MemberExpressionConverterFactory(context),
                new QuoteExpressionConverterFactory(context),
                new LambdaExpressionConverterFactory(context),
                new StringCompareToConverterFactory(context),
                new BinaryExpressionConverterFactory(context),
                new MemberInitExpressionConverterFactory(context),
                new NewExpressionConverterFactory(context),
                new StringFunctionsConverterFactory(context),
                new NavigationMemberExpressionConverterFacotry(context),
                new NavigationJoinCallExpressionConverterFactory(context),
                new FromQueryMethodExpressionConverterFactory(context),
                new WhereQueryMethodExpressionConverterFactory(context),
                new LetLinqKeywordConverterFactory(context),
                //new GroupBySelectMethodCallConverterFactory(context),
                new SelectQueryMethodExpressionConverterFactory(context),
                new AnyQueryMethodExpressionConverterFactory(context),
                new SkipQueryMethodExpressionConverterFactory(context),
                new TakeAfterSkipQueryMethodExpressionConverterFactory(context),
                new TakeQueryMethodExpressionConverterFactory(context),
                new FirstOrDefaultQueryMethodExpressionConverterFactory(context),
                new OrderByQueryMethodExpressionConverterFactory(context),
                new JoinQueryMethodExpressionConverterFactory(context),
                new StandardJoinQueryMethodExpressionConverterFactory(context),
                new GroupByQueryMethodExpressionConverterFactory(context),
                new AggregateStringFunctionConverterFactory(context),
                new AggregateMethodExpressionConverterFactory(context),
                new PagingQueryMethodExpressionConverterFactory(context),
                new UnionQueryMethodExpressionConverterFactory(context),
                new RecursiveUnionQueryMethodExpressionConverterFactory(context),
                new DataSetQueryMethodExpressionConverterFactory(context),
                new ConstantExpressionConverterFactory(context),
                new SelectManyQueryMethodExpressionConverterFactory(context),
                new DefaultIfEmptyExpressionConverterFactory(context),
                new SubQueryNavigationExpressionConverterFactory(context),
                new ConditionalExpressionConverterFactory(context),
                new DistinctQueryMethodExpressionConverterFactory(context),
                new UpdateQueryMethodExpressionConverterFactory(context),
                new DeleteQueryMethodExpressionConverterFactory(context),
                new InValuesExpressionConverterFactory(context),
                new NewArrayExpressionConverterFactory(context),
                new StandaloneSelectQueryMethodExpressionConverterFactory(context),
                new DateFunctionsConverterFactory(context), 
                new ToStringConverterFactory(context),
                new GetValueOrDefaultConverterFactory(context),
                new CastQueryMethodExpressionConverterFactory(context),
                new BulkInsertConverterFactory(context),
                new GuidNewConverterFactory(context),
            };
            this.Factories.AddRange(defaultConverters);
        }
    }
}
