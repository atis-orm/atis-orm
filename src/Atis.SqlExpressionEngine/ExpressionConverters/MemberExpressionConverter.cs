using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.IO;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating converters that handle MemberExpression instances.
    ///     </para>
    /// </summary>
    public class MemberExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public MemberExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MemberExpression memberExpression)
            {
                converter = new MemberExpressionConverter(this.Context, memberExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for handling MemberExpression instances and converting them to SQL expressions.
    ///     </para>
    /// </summary>
    public class MemberExpressionConverter : LinqToNonSqlQueryConverterBase<MemberExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="MemberExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The MemberExpression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public MemberExpressionConverter(IConversionContext context, MemberExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether this instance is a leaf node in the <c>MemberExpression</c> chain.
        ///     </para>
        /// </summary>
        protected virtual bool IsLeafNode => this.ParentConverter?.GetType() != this.GetType();

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var parent = convertedChildren[0];

            SqlDataSourceReferenceExpression parentDataSource;
            ModelPath parentPath = ModelPath.Empty;
            if (parent is SqlDataSourceMemberChainExpression parentChainExpression)
            {
                parentDataSource = parentChainExpression.DataSource;
                parentPath = parentChainExpression.MemberChain;
            }
            else if (parent is SqlDataSourceReferenceExpression parentDsRef)
            {
                parentDataSource = parentDsRef;
            }
            else
            {
                throw new InvalidOperationException($"Parent expression '{parent.GetType().Name}' is not a valid data source reference.");
            }

            // we are not using Extension Method MemberExpression.GetModelPath because
            // the parent might be some other expression but it is in chain
            // for example, x.NavProp1().NavProp2().Field1, the parent of MemberExpression
            // will not be a MemberExpression it will be an InvocationExpression
            var currentPath = parentPath.Append(this.Expression.Member.Name);

            // NOTE: The parentDataSource.Resolve method automatically adjusts the path after resolving.
            // Example: .Select(x => new { C1 = new { T1 = new { V1 = x.Field1 } } }).Select(p2 => p2.C1.T1)
            // In the above case, p2.C1.T1 resolves to a_1.Field1 with the path C1.T1.V1.Field1. However, we cannot return
            // C1.T1.V1.Field1 to the select expression because its path has already been adjusted to C1.T1.
            // The value will be accessed as result.V1 instead of result.C1.T1.V1.Field1. Therefore, the SqlSelectExpression
            // removes C1.T1 from the path and returns a_1.Field1 with the updated path V1.Field1.

            if (this.IsLeafNode)
            {
                var resolvedExpression = parentDataSource.Resolve(currentPath);
                if (resolvedExpression is SqlDataSourceExpression ds)
                {
                    if (ds.TryResolveScalarColumn(out var scalarColumn))
                        return scalarColumn;
                }
                return resolvedExpression;
            }
            else
            {
                var resolvedExpression = parentDataSource.Resolve(currentPath);
                if (resolvedExpression is SqlDataSourceExpression ds)
                {
                    // ModelPath.Empty is crucial here because subsequent calls
                    // must be relative to the data source.
                    // Example: .Select(x => new { x.DerivedTable1.Prop1.Prop2.Field1 })
                    // In the above case, this converter will process `x.DerivedTable1`,
                    // which resolves to a SqlDataSourceExpression. We return the data source
                    // with an empty path, ensuring that the next call (e.g., x.DerivedTable1.Prop1)
                    // is relative to the data source.
                    // For instance, the parent will be a chain expression with a SqlDataSourceExpression
                    // as its parent. This ensures that Prop1 is resolved correctly in the SqlDataSourceExpression.
                    // relative to the data source
                    return new SqlDataSourceMemberChainExpression(ds, ModelPath.Empty);
                }
                return new SqlDataSourceMemberChainExpression(parentDataSource, currentPath);
            }
        }
    }
}
