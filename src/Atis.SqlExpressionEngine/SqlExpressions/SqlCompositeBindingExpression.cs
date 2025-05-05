using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// This class represents a model which is used between converters to pass <see cref="SqlExpressionBinding"/>.
    /// </summary>
    public class SqlCompositeBindingExpression : SqlExpression
    {
        public SqlCompositeBindingExpression(SqlExpressionBinding[] bindings)
        {
            if (!(bindings?.Length > 0))
                throw new ArgumentNullException(nameof(bindings), "Bindings cannot be null or empty.");
            if (bindings.GroupBy(x => x.ModelPath).Any(x => x.Count() > 1))
                throw new ArgumentException("Bindings must have unique model paths.", nameof(bindings));
            if (bindings.Any(x => x.SqlExpression is SqlCompositeBindingExpression))
                throw new ArgumentException("Bindings cannot contain composite bindings.", nameof(bindings));
            this.Bindings = bindings;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.CompsiteBinding;
        public SqlExpressionBinding[] Bindings { get; }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlCompositeBinding(this);
        }

        public SqlCompositeBindingExpression Update(SqlExpressionBinding[] bindings)
        {
            if (this.Bindings.AllEqual(bindings))
                return this;
            return new SqlCompositeBindingExpression(bindings);
        }
    }
}
