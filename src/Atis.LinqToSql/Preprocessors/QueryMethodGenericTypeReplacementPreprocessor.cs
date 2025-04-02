using Atis.Expressions;
using Atis.LinqToSql.Abstractions;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{
    /// <summary>
    ///     <para>
    ///         Represents a preprocessor that replaces generic type parameters in query method calls
    ///         with the appropriate types during the preprocessing phase of expression tree traversal.
    ///     </para>
    /// </summary>
    public partial class QueryMethodGenericTypeReplacementPreprocessor : IExpressionPreprocessor
    {
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryMethodGenericTypeReplacementPreprocessor"/> class.
        /// </summary>
        /// <param name="reflectionService">An instance of <see cref="IReflectionService"/> used for reflection operations.</param>
        public QueryMethodGenericTypeReplacementPreprocessor(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }

        /// <inheritdoc />
        public void BeforeVisit(Expression node, Expression[] expressionsStack)
        {
            // do nothing
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node, Expression[] expressionsStack)
        {
            if (node is MethodCallExpression methodCallExpr && this.reflectionService.IsQueryMethod(node))
            {
                var queryMethodReturnTypeReplacer = new FixLinqMethodCallTSource(this.reflectionService);
                var newMethodCall = queryMethodReturnTypeReplacer.Transform(methodCallExpr);
                return newMethodCall;
            }
            return node;
        }
    }
}
