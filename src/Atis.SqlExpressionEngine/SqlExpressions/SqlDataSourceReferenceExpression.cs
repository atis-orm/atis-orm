using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a reference to a data source in a SQL expression.
    ///     </para>
    ///     <para>
    ///         Caution: this class is intended to be used between the converters and should not be used
    ///         in expression visitor traversal. Usually returned by <c>ParameterExpressionConverter</c>
    ///         and disappears when applying projection in <c>SqlQueryExpression</c>. Thus, final 
    ///         converted <c>SqlQueryExpression</c> should never have the instance of this class anywhere
    ///         within expression tree.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class is crucial when applying projection. Because during projection, if we 
    ///         received a <c>SqlQueryExpression</c> then we need to decide whether it should be directly
    ///         rendered as sub-query or should the columns of this query needs to be selected
    ///         in projection.
    ///     </para>
    ///     <code>
    ///         1. DataSet&lt;Student&gt;().Select(x => new { WholeStudentObject = x});
    ///         2. DataSet&lt;ClassRoom&gt;().Select(x => new { StudentId = DataSet&lt;Student&gt;().OrderBy(y => y.Grading).Select(y => y.StudentId).FirstOrDefault() });
    ///     </code>
    ///     <para>
    ///         <c>x</c> in 1st case (<c>WholeStudentObject = x</c>) is going to be converted to <c>SqlQueryExpression</c> and will be received in Projection.
    ///         Similarly in 2nd case, whole sub-query <c>DataSet&lt;Student&gt;().OrderBy</c> will also be converted to <c>SqlQueryExpression</c>
    ///         and will be received in Projection.
    ///     </para>
    ///     <para>
    ///         In 1st case, Projection needs to pick the <c>Student</c> table columns and select in the projection.
    ///         While in 2nd case, the query needs to be rendered as sub-query within projection.
    ///     </para>
    ///     <para>
    ///         In order for projection system to distinguish between the two cases, we are using this class to detect the reference to the Data Source.
    ///         Therefore, in 1st case, <c>x</c> is not going to be converted to <c>SqlQueryExpression</c> directly rather <c>SqlQueryExpression</c> 
    ///         will be wrapped in <c>SqlDataSourceReferenceExpression</c> and will be received by projection system, which will then select it's columns in projection.
    ///         While in 2nd case, the projection system will directly receive the whole <c>SqlQueryExpression</c> instance and it will then
    ///         simply select it as is and finally will be rendered as sub-query.
    ///     </para>
    /// </remarks>
    public class SqlDataSourceReferenceExpression : SqlReferenceExpression<SqlDataSourceExpression>
    {
        /// <summary>
        ///     <para>
        ///         Creates a new instance of <see cref="SqlDataSourceReferenceExpression" />.
        ///     </para>
        /// </summary>
        /// <param name="dataSource"></param>
        public SqlDataSourceReferenceExpression(SqlDataSourceExpression dataSource)
            : base(dataSource)
        { }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"ds-ref: {this.Reference.DataSourceAlias}";
        }
    }
}
