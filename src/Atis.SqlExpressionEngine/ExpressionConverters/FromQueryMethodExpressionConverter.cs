using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>
    ///         Factory for creating converters for the From query method.
    ///     </para>
    /// </summary>
    public class FromQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="FromQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public FromQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                    methodCallExpression.Method.Name == nameof(QueryExtensions.From) &&
                    methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new FromQueryMethodExpressionConverter(this.Context, methodCallExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter for the From query method.
    ///     </para>
    /// </summary>
    public class FromQueryMethodExpressionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FromQueryMethodExpressionConverter"/> class.
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public FromQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <inheritdoc />
        public override bool TryOverrideChildConversion(Expression sourceExpression, out SqlExpression convertedExpression)
        {
            if (this.Expression.Arguments.FirstOrDefault() == sourceExpression)
            {
                // 1st parameter will be IQueryProvider so we'll simply return dummy
                convertedExpression = this.SqlFactory.CreateLiteral("dummy");
                return true;
            }
            convertedExpression = null;
            return false;
        }

        private Expression _fromSourceExpression;
        private Expression FromSourceExpression
        {
            get
            {
                if (this._fromSourceExpression is null)
                {
                    this._fromSourceExpression = this.Expression.Arguments.Skip(1).FirstOrDefault();
                    if (this._fromSourceExpression is UnaryExpression unaryExpression)
                        this._fromSourceExpression = unaryExpression.Operand;
                    if (this._fromSourceExpression is LambdaExpression lambdaExpression)
                        this._fromSourceExpression = lambdaExpression.Body;
                }
                return this._fromSourceExpression;
            }
        }

        /// <inheritdoc />
        public override bool TryCreateChildConverter(Expression childNode, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> childConverter)
        {
            // childNode can be direct child or grandchild

            if (childNode == this.FromSourceExpression)
            {
                if (childNode is NewExpression newExpression)
                {
                    childConverter = new FromNewExpressionConverter(this.Context, newExpression, converterStack);
                    return true;
                }
                else if (childNode is MemberInitExpression memberInitExpression)
                {
                    childConverter = new FromMemberInitExpressionConverter(this.Context, memberInitExpression, converterStack);
                    return true;
                }
            }
            return base.TryCreateChildConverter(childNode, converterStack, out childConverter);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            SqlQueryExpression result;

            // convertedChildren[0] will be dummy (for IQueryProvider)
            var dataSources = convertedChildren[1];

            if (dataSources is SqlCollectionExpression sqlExpressionCollection)
            {
                var dataSourceList = this.GetDataSources(sqlExpressionCollection);
                result = this.SqlFactory.CreateQueryFromDataSources(dataSourceList);
            }
            else if (dataSources is SqlQueryExpression subQuery)
            {
                var dataSource = this.GetDataSource(subQuery);
                result = this.SqlFactory.CreateQueryFromDataSource(dataSource);
            }
            else
                throw new InvalidOperationException($"From method only supports NewExpression/MemberInitExpression or 'From' MethodCallExpression as argument");

            return result;
        }

        private IEnumerable<SqlDataSourceExpression> GetDataSources(SqlCollectionExpression sqlExpressionCollection)
        {
            var result = new List<SqlDataSourceExpression>();
            foreach (var sqlExpression in sqlExpressionCollection.SqlExpressions)
            {
                var sqlColumnExpr = sqlExpression as SqlDataSourceExpression
                                       ??
                                       throw new InvalidOperationException($"sqlExpression is not {nameof(SqlDataSourceExpression)}");
                var dataSource = this.GetDataSource(sqlColumnExpr);
                result.Add(dataSource);
            }
            return result;
        }

        private SqlDataSourceExpression GetDataSource(SqlExpression source)
        {
            SqlDataSourceExpression fromSource = source as SqlDataSourceExpression;
            SqlQueryExpression sqlQuery = fromSource?.QuerySource as SqlQueryExpression
                                            ??
                                            source as SqlQueryExpression;
            if (sqlQuery != null && sqlQuery.IsTableOnly())
            {
                var firstDataSource = sqlQuery.DataSources.First();
                return this.SqlFactory.CreateFromSource(firstDataSource.QuerySource, fromSource?.ModelPath ?? ModelPath.Empty);
            }
            else if (fromSource != null)
            {
                return this.SqlFactory.CreateFromSource(fromSource.QuerySource, fromSource.ModelPath);
            }
            throw new InvalidOperationException($"source is of type {source.GetType().Name}, which is not supported");
        }
    }
}
