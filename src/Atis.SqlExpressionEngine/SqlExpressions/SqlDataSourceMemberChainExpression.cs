using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// This class represents a model which is used between converters to pass <see cref="SqlDataSourceReferenceExpression"/>
    /// along with it's member chain.
    /// </summary>
    /// <remarks>
    /// This class is not intended to be used directly in the SQL expression tree, therefore, it does not
    /// support the visitor pattern.
    /// </remarks>
    public class SqlDataSourceMemberChainExpression : SqlExpression
    {
        public SqlDataSourceMemberChainExpression(SqlDataSourceReferenceExpression dataSource, ModelPath memberChain)
        {
            this.DataSource = dataSource;
            this.MemberChain = memberChain;
        }

        public override SqlExpressionType NodeType => SqlExpressionType.DatasourceMemberChain;
        public SqlDataSourceReferenceExpression DataSource { get; }
        public ModelPath MemberChain { get; }


        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            throw new InvalidOperationException($"{this.GetType().Name} does not support visitor pattern.");
        }
    }
}
