using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// This class represents a model which is used between converters to pass Data Source along with
    /// it's <see cref="SqlSelectExpression"/>.
    /// </summary>
    /// <remarks>
    /// This class is not intended to be used directly in the SQL expression tree, therefore, it does not
    /// support the visitor pattern.
    /// </remarks>
    public sealed class SqlDataSourceExpression : SqlDataSourceReferenceExpression
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectQuery"></param>
        /// <param name="dataSourceAlias"></param>
        public SqlDataSourceExpression(SqlSelectExpression selectQuery, Guid dataSourceAlias)
        {
            this.SelectQuery = selectQuery;
            this.DataSourceAlias = dataSourceAlias;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.SelectDataSource;
        public SqlSelectExpression SelectQuery { get; }
        public Guid DataSourceAlias { get; }

        
        /// <inheritdoc />
        public override bool TryResolveScalarColumn(out SqlExpression scalarColumnExpression)
        {
            return this.SelectQuery.TryResolveScalarColumnByDataSourceAlias(this.DataSourceAlias, out scalarColumnExpression);
        }

        /// <inheritdoc />
        public override SqlExpression Resolve(ModelPath modelPath)
        {
            return this.SelectQuery.ResolveByDataSourceAlias(this.DataSourceAlias, modelPath);
        }

        /// <inheritdoc />
        public override bool TryResolveExact(ModelPath modelPath, out SqlExpression resolvedExpression)
        {
            return this.SelectQuery.TryResolveExactByDataSourceAlias(this.DataSourceAlias, modelPath, out resolvedExpression);
        }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            // we do NOT go any further
            return this;
            //throw new InvalidOperationException($"{this.GetType().Name} does not support visitor pattern.");
        }
    }
}
