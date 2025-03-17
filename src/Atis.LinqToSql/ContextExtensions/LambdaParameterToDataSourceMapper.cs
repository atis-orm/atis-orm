using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.LinqToSql.ContextExtensions
{
    /// <summary>
    ///     <para>
    ///         Class for mapping lambda parameters to data sources.
    ///     </para>
    ///     <para>
    ///         This class is used as a context extension, which means the instance will be initiated
    ///         at the start of the conversion process and will be alive until the conversion is completed.
    ///     </para>
    ///     <para>
    ///         Different converters can use this class to map lambda parameters to data sources.
    ///     </para>
    ///     <para>
    ///         This class is primarily used by the <see cref="ExpressionConverters.LambdaExpressionConverter"/> to
    ///         map the <c>LambdaExpression</c>'s parameters to the data sources.
    ///     </para>
    /// </summary>
    public class LambdaParameterToDataSourceMapper : ILambdaParameterToDataSourceMapper
    {
        private readonly Dictionary<ParameterExpression, SqlExpression> parameterMap = new Dictionary<ParameterExpression, SqlExpression>();

        /// <inheritdoc />
        public bool TrySetParameterMap(ParameterExpression parameterExpression, SqlExpression sqlExpression)
        {
            if (parameterMap.ContainsKey(parameterExpression))
                return false;
            parameterMap[parameterExpression] = sqlExpression;
            return true;
        }

        /// <inheritdoc />
        public void RemoveParameterMap(ParameterExpression parameterExpression) => parameterMap.Remove(parameterExpression);

        /// <inheritdoc />
        public SqlExpression GetDataSourceByParameterExpression(ParameterExpression parameterExpression)
        {
            if (!parameterMap.TryGetValue(parameterExpression, out var dataSource))
                return null;
            return dataSource;
        }

        /// <inheritdoc />
        public SqlExpression GetQueryByParameterName(string parameterName)
        {
            var parameterExpression = parameterMap.Keys.FirstOrDefault(x => x.Name == parameterName)
                                        ?? throw new InvalidOperationException($"No parameter found with name '{parameterName}'");
            return GetDataSourceByParameterExpression(parameterExpression);
        }
    }
}
