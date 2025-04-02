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
    public partial class QueryMethodGenericTypeReplacementPreprocessor : ExpressionVisitor, IExpressionPreprocessor
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
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression node)
        {
            return this.Visit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var updatedNode = base.VisitMethodCall(node);
            if (updatedNode is MethodCallExpression methodCallExpression &&
                    this.reflectionService.IsQueryMethod(methodCallExpression))
            {
                var queryMethodReturnTypeReplacer = new FixLinqMethodCallTSource(this.reflectionService);
                var newMethodCall = queryMethodReturnTypeReplacer.Transform(methodCallExpression);
                return newMethodCall;
            }
            return updatedNode;
        }
    }
}
