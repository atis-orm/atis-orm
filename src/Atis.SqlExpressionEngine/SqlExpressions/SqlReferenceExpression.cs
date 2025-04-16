using Atis.SqlExpressionEngine.ExpressionConverters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public interface ISqlReferenceExpression
    {
        /// <summary>
        ///     <para>
        ///         Gets the reference to the SQL expression.
        ///     </para>
        /// </summary>
        SqlExpression Reference { get; }
    }

    public class SqlReferenceExpression<T> : SqlExpression, ISqlReferenceExpression where T : SqlExpression
    {
        public SqlReferenceExpression(T reference)
        {
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.Reference;

        /// <summary>
        ///     <para>
        ///         
        ///     </para>
        /// </summary>
        public T Reference { get; }

        SqlExpression ISqlReferenceExpression.Reference => this.Reference;

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlReferenceExpression(this);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"ref: {this.Reference}";
        }
    }
}
