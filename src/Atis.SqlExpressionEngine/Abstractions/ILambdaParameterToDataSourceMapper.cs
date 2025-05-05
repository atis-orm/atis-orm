using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.Abstractions
{
    public interface ILambdaParameterToDataSourceMapper
    {
        SqlExpression GetDataSourceByParameterExpression(ParameterExpression parameterExpression);
        SqlExpression GetQueryByParameterName(string parameterName);
        void RemoveParameterMap(ParameterExpression parameterExpression);
        bool TrySetParameterMap(ParameterExpression parameterExpression, SqlExpression sqlExpression);
    }
}