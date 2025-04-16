using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{
    /// <summary>
    ///     <para>  
    ///         Factory class for creating converters for RecursiveUnion query method expressions.
    ///     </para>
    /// </summary>
    public class RecursiveUnionQueryMethodExpressionConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="RecursiveUnionQueryMethodExpressionConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public RecursiveUnionQueryMethodExpressionConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpr &&
                    methodCallExpr.Method.Name == nameof(QueryExtensions.RecursiveUnion) &&
                    methodCallExpr.Method.DeclaringType == typeof(QueryExtensions))
            {
                converter = new RecursiveUnionQueryMethodExpressionConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for RecursiveUnion query method expressions.
    ///     </para>
    /// </summary>
    public class RecursiveUnionQueryMethodExpressionConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        private SqlQueryExpression anchorQuery;
        private readonly ILambdaParameterToDataSourceMapper parameterMap;

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="RecursiveUnionQueryMethodExpressionConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public RecursiveUnionQueryMethodExpressionConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
            this.parameterMap = this.Context.GetExtensionRequired<ILambdaParameterToDataSourceMapper>();
        }

        /// <inheritdoc />
        public override void OnConversionCompletedByChild(ExpressionConverterBase<Expression, SqlExpression> childConverter, Expression childNode, SqlExpression convertedExpression)
        {
            if (childNode == this.Expression.Arguments[0])              // if 1st arg (source query) is converted
            {
                var sqlQuery = this.GetSqlQueryRequired(convertedExpression);
                // here we are creating a copy of the actual query, because it might be changed later on, for example,
                // in-case if user directly used the query to join the other source in Recursive Member part, then
                // the other source will be joined in the main query, while we need the main query without join
                // so that it can be used in the 1st part of Union All (anchor)
                this.anchorQuery = sqlQuery.CreateCopy();

                // we are wrapping the anchor query regardless if it's required or not,
                // usually before RecursiveUnion, probably user have done some Where / Select,
                // and during the RecursiveUnion, user will do InnerJoin, therefore, InnerJoin
                // will cause auto wrapping because of previous Where / Select, but we are NOT
                // going to relay on that, we will wrap whether it's required or not.
                sqlQuery.ApplyAutoProjection();

                // since this class is not inheriting from QueryMethodCallExpressionConverterBase
                // therefore, we need to map the Lambda Parameters manually
                var arg1LambdaParam = this.GetArg1LambdaParameterRequired();
                this.parameterMap.TrySetParameterMap(arg1LambdaParam, sqlQuery);
            }
        }

        /// <inheritdoc />
        public override void OnAfterVisit()
        {
            // since this class is not inheriting from QueryMethodCallExpressionConverterBase
            // therefore, we need to remove the Lambda Parameters manually
            var arg1LambdaParam = GetArg1LambdaParameterRequired();
            this.parameterMap.RemoveParameterMap(arg1LambdaParam);
        }

        private ParameterExpression GetArg1LambdaParameterRequired()
        {
            var arg1 = this.Expression.Arguments[1];
            if (arg1 is UnaryExpression unaryExpr)
                arg1 = unaryExpr.Operand;
            var arg1Lambda = arg1 as LambdaExpression
                                ??
                                throw new InvalidOperationException("Expected LambdaExpression as 2nd argument of RecursiveUnion method.");
            return arg1Lambda.Parameters[0];
        }

        private SqlQueryExpression GetSqlQueryRequired(SqlExpression sqlExpression)
        {
            var sqlQuery =  sqlExpression as SqlQueryExpression
                            ??
                            throw new ArgumentException("Unable to convert to SqlQueryExpression", nameof(sqlExpression));
            return sqlQuery;
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // RecursiveUnion(IQueryable<T> oldQuery, Expression<Func<IQueryable<T>, IQueryable<T>> recursiveMember)
            //                                 |                                                            |
            //                               arg0                                                         arg1
            // Call: anchorQuery.RecursiveUnion(anchorMember => .... recursive member query using anchorMember ....)
            var anchorMember = this.GetSqlQueryRequired(convertedChildren[0]);                     // arg0
            var recursiveMember = convertedChildren[1] as SqlQueryExpression
                                    ?? throw new InvalidOperationException("Expected a query expression on the stack.");
            var recursiveMemberIsNotMainQuery = anchorMember != recursiveMember;
            var cteAlias = Guid.NewGuid();
            SqlQueryExpression unionQuery;
            if (recursiveMemberIsNotMainQuery)
            {
                // if we are here, it means join was *NOT performed* directly on anchor member, e.g.
                //
                //                                here the recursive member is starting with "employees" which is not anchor member
                //                                                       \
                // employeeAnchorQuery.RecursiveUnion(employeeAnchor => employees.InnerJoin(employeeAnchor, ...
                //                                                 \___________________________/
                //                                                                          /
                //                                           instead anchor member is used here in 1st arg of InnerJoin
                // Which means recursive member is a totally new query using anchor member as joined data source.
                // So, we are going to replace the anchor member with CTE reference, e.g.
                //      select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
                //      from	Employee as a_3	
                //      		inner join (
                //                  ----  this is anchor member  ----
                //      		        select	a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.ManagerId as ManagerId	
                //                      from    Employee as a_1
                //                      where   (a_1.ManagerId = null)
                //                  -------------------------------------------------------------------
                //      		) as a_4 on (a_4.EmployeeId = a_3.ManagerId)
                // below replacement will replace anchor member with CTE reference, like this,
                //      select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
                //      from	Employee as a_3	
                //      		inner join cte_5 as a_4 on (a_4.EmployeeId = a_3.ManagerId)
                //
                unionQuery = this.ReplaceMainQueryWithCteReference(anchorMember, recursiveMember, cteAlias);
            }
            else
            {
                // if we are here, it means join was performed directly on anchor member, e.g.
                //
                //                                                              we are using other data source in join
                //                                                                                \
                // employeeAnchorQuery.RecursiveUnion(employeeAnchor => employeeAnchor.InnerJoin(employees, ...
                //                                                 \___________/
                //                                                    /
                //                                here the recursive member is starting with "employeeAnchor" which is anchor member
                // Which means recursive member is not a new query, it's same as the current Context's Query.
                // So, we'll change the initial data source in current query to CTE Reference, e.g.,
                //      select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
                //      from    (
                //           ----  this is anchor member  ----
                //            select a_1.EmployeeId as EmployeeId, a_1.Name as Name, a_1.ManagerId as ManagerId
                //            from Employee as a_1
                //            where (a_1.ManagerId = null)
                //           -------------------------------------------------------------------
                //      ) as a_2
                //              inner join Employee as a_3 on (a_2.EmployeeId = a_3.ManagerId)
                //
                // below we are creating new data source to Update "a_2" with CTE reference, like this,
                // 
                //      select	a_3.EmployeeId as EmployeeId, a_3.Name as Name, a_3.ManagerId as ManagerId	
                //      from    cte_4 as a_2
                //              inner join Employee as a_3 on(a_2.EmployeeId = a_3.ManagerId)
                //
                var cteReference = this.SqlFactory.CreateCteReference(cteAlias);
                // initial data source is the one which needs to be changed to CTE reference
                // other data sources will be linking to the same data source, that's why
                // it's crucial to use the same DataSourceAlias as all other joins and column selections
                // will be using same DataSourceAlias
                var newDataSource = this.SqlFactory.CreateDataSourceForCteReference(anchorMember.InitialDataSource.DataSourceAlias, cteReference);

                unionQuery = anchorMember.Update(newDataSource, anchorMember.Joins, anchorMember.WhereClause, anchorMember.GroupBy, anchorMember.Projection, anchorMember.OrderBy, anchorMember.Top, anchorMember.CteDataSources, anchorMember.HavingClause, anchorMember.Unions);
            }

            var cteQuery = this.CreateCteQuery(unionQuery, cteAlias);

            return cteQuery;
        }

        /// <summary>
        ///     <para>
        ///         Creates a CTE query from the given union query and alias.
        ///     </para>
        /// </summary>
        /// <param name="unionQuery">The union query.</param>
        /// <param name="cteAlias">The CTE alias.</param>
        /// <returns>The created CTE query.</returns>
        protected virtual SqlQueryExpression CreateCteQuery(SqlQueryExpression unionQuery, Guid cteAlias)
        {
            this.anchorQuery.ApplyAutoProjection();
            unionQuery.ApplyAutoProjection();
            this.anchorQuery.ApplyUnion(unionQuery, unionAll: true);

            var cteQuery = this.SqlFactory.CreateCteQuery(cteAlias, this.anchorQuery);

            return cteQuery;
        }

        private SqlQueryExpression ReplaceMainQueryWithCteReference(SqlQueryExpression mainQuery, SqlQueryExpression joinedQuery, Guid cteAlias)
        {
            var cteReferenceReplacementVisitor = new CteReferenceReplacementVisitor(mainQuery, cteAlias, this.SqlFactory);
            return cteReferenceReplacementVisitor.Visit(joinedQuery)
                    as SqlQueryExpression
                    ??
                    throw new InvalidOperationException("Expected SqlQueryExpression");
        }

        
        private class CteReferenceReplacementVisitor : SqlExpressionVisitor
        {
            private readonly SqlQueryExpression queryToReplace;
            private readonly Guid cteAlias;
            private readonly ISqlExpressionFactory sqlFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="CteReferenceReplacementVisitor"/> class.
            /// </summary>
            /// <param name="queryToReplace">The query to be replaced.</param>
            /// <param name="cteAlias">The CTE alias.</param>
            /// <param name="sqlFactory"></param>
            public CteReferenceReplacementVisitor(SqlQueryExpression queryToReplace, Guid cteAlias, ISqlExpressionFactory sqlFactory)
            {
                this.queryToReplace = queryToReplace;
                this.cteAlias = cteAlias;
                this.sqlFactory = sqlFactory;
            }

            /// <inheritdoc />
            protected internal override SqlExpression VisitSqlQueryExpression(SqlQueryExpression sqlQueryExpression)
            {
                if (sqlQueryExpression == this.queryToReplace)
                {
                    var cteReference = this.sqlFactory.CreateCteReference(this.cteAlias);
                    var newDataSource = this.sqlFactory.CreateDataSourceForCteReference(sqlQueryExpression.InitialDataSource.DataSourceAlias, cteReference);
                    var updatedSqlQuery = sqlQueryExpression.Update(newDataSource, sqlQueryExpression.Joins, sqlQueryExpression.WhereClause, sqlQueryExpression.GroupBy, sqlQueryExpression.Projection, sqlQueryExpression.OrderBy, sqlQueryExpression.Top, sqlQueryExpression.CteDataSources, sqlQueryExpression.HavingClause, sqlQueryExpression.Unions);
                    return updatedSqlQuery;
                }
                return base.VisitSqlQueryExpression(sqlQueryExpression);
            }

            /// <inheritdoc />
            protected internal override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
            {
                if (sqlDataSourceExpression.QuerySource == this.queryToReplace)
                {
                    var cteReference = this.sqlFactory.CreateCteReference(this.cteAlias);
                    var newDataSource = this.sqlFactory.CreateDataSourceForCteReference(sqlDataSourceExpression.DataSourceAlias, cteReference);
                    // when new data source is returned here, it will be returned back to VisitSqlQueryExpression method
                    // which will call Update method of SqlQueryExpression because of new SqlDataSourceExpression, in turn,
                    // SqlQueryExpression's Update method will set the ParentSqlQuery in this new SqlDataSourceExpression instance.
                    return newDataSource;
                }
                return base.VisitSqlDataSourceExpression(sqlDataSourceExpression);
            }
        }
    }
}
