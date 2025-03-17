using Atis.Expressions;
using Atis.LinqToSql.Infrastructure;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Preprocessors
{

    /// <summary>
    ///     <para>
    ///         The <c>SpecificationCallRewriterPreprocessor</c> class is responsible for preprocessing
    ///         expression trees to replace calls to <c>IsSatisfiedBy</c> methods in specifications
    ///         with the actual predicate expressions defined in those specifications.
    ///     </para>
    ///     <para>
    ///         This class implements the <see cref="IExpressionPreprocessor"/> interface and uses
    ///         the <see cref="SpecificationCallRewriterVisitor"/> to traverse and modify the expression tree.
    ///     </para>
    /// </summary>
    public partial class SpecificationCallRewriterPreprocessor : IExpressionPreprocessor
    {
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpecificationCallRewriterPreprocessor"/> class.
        /// </summary>
        /// <param name="reflectionService">The reflection service used for accessing type and member information.</param>
        public SpecificationCallRewriterPreprocessor(IReflectionService reflectionService)
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
            var isSatisfiedCallReplacer = new SpecificationCallRewriterVisitor(this.reflectionService);
            return isSatisfiedCallReplacer.Visit(node);
        }
    }
}
