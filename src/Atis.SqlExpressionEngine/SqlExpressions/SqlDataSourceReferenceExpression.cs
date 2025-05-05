using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// Represents an expression that can be passed between converters to resolve a Model Path.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When the <see cref="System.Linq.Expressions.ParameterExpression"/> is converted it is usually
    ///         converted to <see cref="SqlSelectExpression"/>, however, in some cases it might be converted
    ///         to <see cref="SqlDataSourceExpression"/> in-case if the parameter is mapped to a data source.
    ///     </para>
    ///     <para>
    ///         This expression helps the converters to recognize whether the mapped SqlExpression is a Select Query or
    ///         a Data Source.
    ///     </para>
    /// </remarks>
    public abstract class SqlDataSourceReferenceExpression : SqlExpression
    {
        public abstract bool TryResolveScalarColumn(out SqlExpression scalarColumnExpression);
        public abstract SqlExpression Resolve(ModelPath modelPath);
        public abstract bool TryResolveExact(ModelPath modelPath, out SqlExpression resolvedExpression);
    }
}
