using Atis.Expressions;
using Atis.LinqToSql.Postprocessors;
using Atis.LinqToSql.SqlExpressions;
using System.Collections.Generic;

namespace Atis.LinqToSql
{
    public class PostprocessorProvider : IPostprocessorProvider
    {
        private readonly int maxIterations;
        protected List<IPostprocessor> PostProcessors { get; } = new List<IPostprocessor>();
        public PostprocessorProvider(ISqlExpressionFactory sqlFactory, IEnumerable<IPostprocessor> postprocessors, int maxIterations = 50)
        {
            if (postprocessors != null)
                this.PostProcessors.AddRange(postprocessors);
            this.PostProcessors.Add(new CteFixPostProcessor(sqlFactory));
            this.PostProcessors.Add(new CteCrossJoinPostprocessor(sqlFactory));
            this.maxIterations = maxIterations;
        }

        public SqlExpression Process(SqlExpression sqlExpression)
        {
            bool expressionChanged;
            int iterations = 0;

            do
            {
                expressionChanged = false;

                foreach (var postProcessor in this.PostProcessors)
                {
                    postProcessor.Initialize();
                    var newSqlExpression = postProcessor.Process(sqlExpression);

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
