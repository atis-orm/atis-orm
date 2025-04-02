using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.Expressions
{
    public class ExpressionPreprocessorProvider
    {
        private readonly int maxIterations;
        protected List<IExpressionPreprocessor> ExpressionPreprocessors { get; } = new List<IExpressionPreprocessor>();
        public ExpressionPreprocessorProvider(IEnumerable<IExpressionPreprocessor> postprocessors, int maxIterations = 50)
        {
            if (postprocessors != null)
                this.ExpressionPreprocessors.AddRange(postprocessors);
            this.maxIterations = maxIterations;
        }

        public Expression Preprocess(Expression expression)
        {
            bool expressionChanged;
            int iterations = 0;

            do
            {
                expressionChanged = false;

                foreach (var postProcessor in this.ExpressionPreprocessors)
                {
                    postProcessor.Initialize();
                    var newSqlExpression = postProcessor.Preprocess(expression);

                    if (newSqlExpression != expression)
                    {
                        expression = newSqlExpression;
                        expressionChanged = true;
                    }
                }

                iterations++;

                if (iterations >= this.maxIterations)
                {
                    throw new PreprocessingThresholdExceededException(this.maxIterations);
                }

            } while (expressionChanged);

            return expression;
        }
    }
}
