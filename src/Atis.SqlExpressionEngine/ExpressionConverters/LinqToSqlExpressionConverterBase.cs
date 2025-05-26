using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    public interface ILinqToSqlExpressionConverterBase
    {
        bool IsQueryConverter { get; }
        bool IsChainedQueryArgument(Expression childNode);
    }

    /// <summary>
    ///     <para>
    ///         Abstract base class for converting LINQ expressions to SQL expressions.
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">The type of the source expression to be converted.</typeparam>
    public abstract class LinqToSqlExpressionConverterBase<TSource> : ExpressionConverterBase<Expression, SqlExpression>, ILinqToSqlExpressionConverterBase where TSource : Expression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="LinqToNonSqlQueryConverterBase{TSource}"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The source expression to be converted.</param>
        /// <param name="converters">The stack of converters representing the parent chain for context-aware conversion.</param>
        protected LinqToSqlExpressionConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpression>[] converters)
            : base(expression, converters)
        {
            this.Context = context;
            this.SqlFactory = this.Context.GetExtensionRequired<ISqlExpressionFactory>();
            this.ReflectionService = this.Context.GetExtensionRequired<IReflectionService>();
            this.parameterMapper = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
            this.logger = this.Context.GetExtension<ILogger>();
        }

        private readonly ILambdaParameterToDataSourceMapper parameterMapper;
        private readonly ILogger logger;

        protected void LogIndent() => logger?.Indent();
        protected void LogUnindent() => logger?.Unindent();
        protected void Log(string text) => logger?.Log(text);

        /// <summary>
        ///     <para>
        ///         Gets the conversion context for the current conversion process.
        ///     </para>
        /// </summary>
        public IConversionContext Context { get; }
        /// <summary>
        /// 
        /// </summary>
        public ISqlExpressionFactory SqlFactory { get; }
        public IReflectionService ReflectionService { get; }
        protected HashSet<ParameterExpression> MappedParameters { get; } = new HashSet<ParameterExpression>();

        /// <summary>
        ///     <para>
        ///         Gets the source expression that is currently being converted.
        ///     </para>
        /// </summary>
        public new TSource Expression => (TSource)base.Expression;

        public abstract bool IsQueryConverter { get; }
        public abstract bool IsChainedQueryArgument(Expression childNode);
        public abstract SqlExpression Convert(SqlExpression[] convertedChildren);

        //public void UpdateQueryShape(SqlSelectExpression selectQuery, SqlExpression newQueryShape)
        //{
        //    if (selectQuery is null)
        //        throw new ArgumentNullException(nameof(selectQuery));
        //    if (newQueryShape is null)
        //        throw new ArgumentNullException(nameof(newQueryShape));

        //    this.UpdateQueryShape(selectQuery.QueryShape, newQueryShape, selectQuery);
        //}

        //public void UpdateQueryShape(SqlExpression oldQueryShape, SqlExpression newQueryShape, SqlSelectExpression selectQuery)
        //{
        //    if (oldQueryShape is null)
        //        throw new ArgumentNullException(nameof(oldQueryShape));
        //    if (newQueryShape is null)
        //        throw new ArgumentNullException(nameof(newQueryShape));

        //    this.ParameterMapper.UpdateExpression(oldQueryShape, newQueryShape);
        //    selectQuery?.UpdateModelBinding(newQueryShape);
        //}
        protected void MapParameter(ParameterExpression parameterExpression, Func<SqlExpression> sqlExpressionExtractor)
        {
            this.parameterMapper.RemoveParameterMap(parameterExpression);
            this.parameterMapper.TrySetParameterMap(parameterExpression, sqlExpressionExtractor);
            this.MappedParameters.Add(parameterExpression);
        }

        public virtual void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            // no default implementation
        }

        /// <inheritdoc />
        public sealed override SqlExpression TransformConvertedChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (this.IsQueryConverter)      // if this method is a query converter e.g. Select
            {
                // the child that returned the conversion might return a SqlSelectExpression or might NOT return a SqlSelectExpression
                // Case: the argument that is returning the result is a Chained Query Argument
                //          in this case the result must be a SqlSelectExpression if it's NOT then we'll convert it to SqlSelectExpression
                // Case: the child converter that is returning the SqlSelectExpression is a Query Converter? if NOT
                //          then we'll check if convertedExpression is a SqlSelectExpression then we'll convert it to SqlDerivedTable

                // as expected the resulting expression is SqlSelectExpression but
                if (convertedExpression is SqlSelectExpression selectQuery)
                {
                    // if child converter is NOT a query converter then it should NOT be sending the SqlSelectExpression
                    if (!(childConverter is ILinqToSqlExpressionConverterBase)
                        ||
                        (childConverter is ILinqToSqlExpressionConverterBase cc && !cc.IsQueryConverter))
                    {
                        var selectCopy = selectQuery.CreateCopy();

                        var childNodeType = childNode.ExtractLambda()?.Body.Type ?? childNode.Type;
                        if (this.ReflectionService.IsGroupingType(childNodeType))
                        {
                            // it means we are coming from GroupBy method
                            // we are going to map the select query itself
                            // need to convert the source query to derived table with grouping removed
                            var groupingExpression = selectCopy.GroupByClause;
                            selectCopy.RemoveGrouping();
                            selectCopy.ApplyWhereMultipleFields(selectQuery.GroupByClause, groupingExpression);
                        }
                        var derivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(selectCopy);
                        convertedExpression = derivedTable;
                    }
                }

                // if converted child is a Chained Query Argument usually the 1st one
                if (this.IsChainedQueryArgument(childNode))
                {
                    if (!(convertedExpression is SqlSelectExpression))
                    {
                        // the converted result should be a SqlSelectExpression so we'll convert it to SqlSelectExpression
                        if (convertedExpression is SqlQuerySourceExpression querySource)
                        {
                            convertedExpression = this.SqlFactory.CreateSelectQuery(querySource);
                        }
                        else if (convertedExpression is SqlQueryableExpression queryable)
                        {
                            convertedExpression = this.SqlFactory.CreateSelectQuery(queryable.Query);
                        }
                        else
                            throw new InvalidOperationException($"Converter '{this.GetType().Name}' has been marked as Query Converter, also the converter is suggesting that childNode '{childNode}' will be a '{nameof(SqlSelectExpression)}' but it's not, so the core engine is trying to create '{nameof(SqlSelectExpression)}' from converted child '{convertedExpression.GetType().Name}' but it's not '{nameof(SqlQuerySourceExpression)}'. The child converter '{childConverter.GetType().Name}' should convert the node '{childNode}' to either '{nameof(SqlSelectExpression)}' or '{nameof(SqlQuerySourceExpression)}'.");
                    }
                }
                else
                    this.Log($"Child converted ({convertedExpression.NodeType}): {childNode}");
            }
            this.OnConversionCompletedByChild(childConverter, childNode, convertedExpression);
            return convertedExpression;
        }

        /// <inheritdoc />
        public sealed override SqlExpression CreateFromChildren(SqlExpression[] convertedChildren)
        {
            var convertedResult = this.Convert(convertedChildren);

            if (this.IsQueryConverter)
            {
                var text = GetStringRepresentation();
                this.LogUnindent();
                this.Log($"Exiting Converter ({convertedResult.NodeType}): {text}");
            }

            if (convertedResult is SqlQueryableExpression queryable)
                convertedResult = queryable.Query;
            if (this.ParentConverter is null        // if this is the top-most node
                ||                                  //  or
                                                    // if the parent is not a query converter
                this.ParentConverter is ILinqToSqlExpressionConverterBase parentConverter &&
                    (
                        !parentConverter.IsQueryConverter ||
                        // it means parentConverter is a query converter but we cannot treat this child
                        // as chained query if it's not the one which should be converted
                        !parentConverter.IsChainedQueryArgument(this.Expression)
                    )
                )
            {
                // if this converter is a Query Converter e.g. Select method
                if (this.IsQueryConverter)
                {
                    // now we need to close the query, because either this is the top-most node or
                    // parent is not a query converter or parent is the query converter but this
                    // child is not the chained query parameter
                    // e.g. Join(querySource, otherDataSource, PK selection, FK selection, new shape)
                    // in above call `otherDataSource` will also be translated to SqlSelectExpression but
                    // it should be closed after conversion of that node because `otherDataSource` is not the
                    // chained argument to parent (Join)
                    if (convertedResult is SqlSelectExpression selectQuery)
                    {
                        var derivedTable = this.SqlFactory.ConvertSelectQueryToDeriveTable(selectQuery);
                        convertedResult = derivedTable;
                    }
                }
            }
            return convertedResult;
        }


        private string GetStringRepresentation()
        {
            if (this is DataSetQueryMethodExpressionConverter)
            {
                var type = this.Expression.Type.GetGenericArguments().FirstOrDefault();
                return $"DataSet<{type?.Name ?? "Unknown"}>";
            }
            else if (this.Expression is MethodCallExpression methodCall)
                return methodCall.Method.Name;
            else if (this.Expression is NavigationJoinCallExpression navJoin)
                return "NavJoin";
            else if (this.Expression is ParameterExpression paramExpression)
                return $"ParamExpression {paramExpression.Name}";
            else if (this.Expression is MemberExpression memberExpression)
                return memberExpression.ToString();
            else
                return this.GetType().Name;
        }

        /// <inheritdoc />
        public override void OnBeforeVisit()
        {
            if (this.IsQueryConverter)
            {
                this.Log($"Entering Converter: {GetStringRepresentation()}");
                this.LogIndent();
            }
            base.OnBeforeVisit();
        }

        /// <inheritdoc />
        public override void OnAfterVisit()
        {
            foreach (var mappedParameter in this.MappedParameters)
            {
                this.parameterMapper.RemoveParameterMap(mappedParameter);
            }
            base.OnAfterVisit();
        }
    }

    public abstract class LinqToSqlQueryConverterBase<TSource> : LinqToSqlExpressionConverterBase<TSource> where TSource : Expression
    {
        protected LinqToSqlQueryConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public sealed override bool IsQueryConverter => true;
    }

    public abstract class LinqToNonSqlQueryConverterBase<TSource> : LinqToSqlExpressionConverterBase<TSource> where TSource : Expression
    {
        protected LinqToNonSqlQueryConverterBase(IConversionContext context, TSource expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        public sealed override bool IsQueryConverter => false;
        public sealed override bool IsChainedQueryArgument(Expression childNode) => false;
    }
}
