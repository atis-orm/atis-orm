using System.Linq.Expressions;

namespace Atis.Expressions
{
    public interface IExpressionPreprocessorProvider
    {
        Expression Preprocess(Expression expression);
    }
}