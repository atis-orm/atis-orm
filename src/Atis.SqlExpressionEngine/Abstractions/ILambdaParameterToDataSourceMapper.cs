using Atis.SqlExpressionEngine.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.Abstractions
{
    /// <summary>
    ///     <para>
    ///         Interface for mapping lambda parameters to data sources.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This interface is used as Context Extension, which means the instance will be initiated
    ///         in the start of conversion process and will be alive until the conversion is completed.
    ///     </para>
    ///     <para>
    ///         Different converters can use this interface to map lambda parameters to data sources.
    ///     </para>
    ///     <para>
    ///         This interface is primarily used by the <see cref="ExpressionConverters.LambdaExpressionConverter"/> to
    ///         map the <c>LambdaExpression</c>'s parameters to the data sources.
    ///     </para>
    /// </remarks>
    public interface ILambdaParameterToDataSourceMapper
    {
        /// <summary>
        ///     <para>
        ///         Gets the data source associated with the specified parameter expression.
        ///     </para>
        /// </summary>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <returns>The associated data source as a <see cref="SqlExpression"/>.</returns>
        SqlExpression GetDataSourceByParameterExpression(ParameterExpression parameterExpression);

        /// <summary>
        ///     <para>
        ///         Gets the query associated with the specified parameter name.
        ///     </para>
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>The associated query as a <see cref="SqlExpression"/>.</returns>
        SqlExpression GetQueryByParameterName(string parameterName);

        /// <summary>
        ///     <para>
        ///         Removes the mapping for the specified parameter expression.
        ///     </para>
        /// </summary>
        /// <param name="parameterExpression">The parameter expression.</param>
        void RemoveParameterMap(ParameterExpression parameterExpression);

        /// <summary>
        ///     <para>
        ///         Tries to set the mapping for the specified parameter expression to the given SQL expression.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the mapping is already defined then this method will not set the mapping.
        ///     </para>
        /// </remarks>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="sqlExpression">The SQL expression to map to.</param>
        bool TrySetParameterMap(ParameterExpression parameterExpression, SqlExpression sqlExpression);
    }
}