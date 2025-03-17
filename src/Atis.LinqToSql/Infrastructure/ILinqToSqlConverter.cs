using Atis.LinqToSql.SqlExpressions;
using System.Linq.Expressions;

namespace Atis.LinqToSql.Infrastructure
{

    /// <summary>
    ///     <para>
    ///         Defines a method to convert a LINQ expression to a SQL expression.
    ///     </para>
    /// </summary>
    public interface ILinqToSqlConverter
    {
        /// <summary>
        ///     <para>
        ///         Converts the specified LINQ expression to a SQL expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">
        ///     <para>
        ///         The LINQ expression to convert.
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para>
        ///         The converted SQL expression.
        ///     </para>
        /// </returns>
        SqlExpression Convert(Expression expression);
    }
}