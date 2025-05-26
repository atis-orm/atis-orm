using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.Services
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
        private readonly Dictionary<ParameterExpression, Func<SqlExpression>> parameterMap = new Dictionary<ParameterExpression, Func<SqlExpression>>();

        /// <inheritdoc />
        public bool TrySetParameterMap(ParameterExpression parameterExpression, Func<SqlExpression> sqlExpressionExtractor)
        {
            if (parameterMap.ContainsKey(parameterExpression))
                return false;
            parameterMap[parameterExpression] = sqlExpressionExtractor;
            return true;
        }

        /// <inheritdoc />
        public void RemoveParameterMap(ParameterExpression parameterExpression) => parameterMap.Remove(parameterExpression);

        /// <inheritdoc />
        public SqlExpression GetDataSourceByParameterExpression(ParameterExpression parameterExpression)
        {
            if (!parameterMap.TryGetValue(parameterExpression, out var sqlExpressionExtractor))
                return null;
            return sqlExpressionExtractor();
        }

        /// <inheritdoc />
        public SqlExpression GetQueryByParameterName(string parameterName)
        {
            var parameterExpression = parameterMap.Keys.FirstOrDefault(x => x.Name == parameterName)
                                        ?? throw new InvalidOperationException($"No parameter found with name '{parameterName}'");
            return GetDataSourceByParameterExpression(parameterExpression);
        }

        //public void UpdateExpression(SqlExpression oldSqlExpression, SqlExpression newSqlExpression)
        //{
        //    var matchedKeys = this.parameterMap.Where(x => x.Value == oldSqlExpression).ToArray();
        //    foreach (var kv in matchedKeys)
        //    {
        //        this.parameterMap[kv.Key] = newSqlExpression;
        //    }
        //}
    }
}
