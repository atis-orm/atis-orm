using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using Atis.LinqToSql.Exceptions;
using Atis.LinqToSql.Postprocessors;
using Atis.LinqToSql.SqlExpressions;
using System.Collections.Generic;

namespace Atis.LinqToSql.Services
{
    public class SqlExpressionPostprocessorProvider : ISqlExpressionPostprocessorProvider
    {
        private readonly int maxIterations;
        protected List<ISqlExpressionPostprocessor> PostProcessors { get; } = new List<ISqlExpressionPostprocessor>();
        public SqlExpressionPostprocessorProvider(ISqlExpressionFactory sqlFactory, IEnumerable<ISqlExpressionPostprocessor> postprocessors, int maxIterations = 50)
        {
            if (postprocessors != null)
                this.PostProcessors.AddRange(postprocessors);
            this.PostProcessors.Add(new CteFixPostprocessor(sqlFactory));
            this.PostProcessors.Add(new CteCrossJoinPostprocessor(sqlFactory));
            this.maxIterations = maxIterations;
        }

        public SqlExpression Postprocess(SqlExpression sqlExpression)
        {
            bool expressionChanged;
            int iterations = 0;

            do
            {
                expressionChanged = false;

                foreach (var postProcessor in this.PostProcessors)
                {
                    postProcessor.Initialize();
                    var newSqlExpression = postProcessor.Postprocess(sqlExpression);

                    if (newSqlExpression != sqlExpression)
                    {
                        sqlExpression = newSqlExpression;
                        expressionChanged = true;
                    }
                }

                iterations++;

                if (iterations >= this.maxIterations)
                {
                    throw new PostprocessingThresholdExceededException(this.maxIterations);
                }

            } while (expressionChanged);

            return sqlExpression;
        }
    }
}
