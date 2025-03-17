using System;

namespace Atis.LinqToSql.SqlExpressions
{
    // TODO: merge with SqlDataSourceExpression using node type

    /// <summary>
    ///     <para>
    ///         Represents Sql Data Source Expression extracted from <see cref="QueryExtensions.From{T}(System.Linq.IQueryProvider, System.Linq.Expressions.Expression{Func{T}})"/>
    ///         method call.
    ///     </para>
    /// </summary>
    public class SqlFromSourceExpression : SqlDataSourceExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlFromSourceExpression"/> class with the specified data source alias, data source, and model path.
        ///     </para>
        /// </summary>
        /// <param name="dataSourceAlias">The alias of the data source.</param>
        /// <param name="dataSource">The data source expression.</param>
        /// <param name="modelPath">The model path associated with the data source.</param>
        public SqlFromSourceExpression(Guid dataSourceAlias, SqlQuerySourceExpression dataSource, ModelPath modelPath) 
            : base(dataSourceAlias, dataSource, modelPath: modelPath, tag: null)
        {
        }

        public SqlFromSourceExpression(SqlQuerySourceExpression dataSource, ModelPath modelPath) : base(dataSource, modelPath)
        {
        }

        public SqlFromSourceExpression(Guid dataSourceAlias, SqlQuerySourceExpression daaSource, ModelPath modelPath, string tag) 
            : base(dataSourceAlias, daaSource, modelPath, tag)
        {
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.FromSource;

        public SqlDataSourceExpression Update(SqlQuerySourceExpression dataSource)
        {
            if (dataSource == DataSource)
            {
                return this;
            }
            return new SqlFromSourceExpression(this.DataSourceAlias, dataSource, this.ModelPath, this.Tag);
        }
    }
}
