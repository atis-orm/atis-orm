using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.SqlExpressionEngine.Internal
{
    /// <summary>
    ///     <para>
    ///         Extracts the Data Source and Property Info mapping.
    ///     </para>
    ///     <para>
    ///         Not intended to be used by end user.
    ///     </para>
    /// </summary>
    public class DataSourcePropertyInfoExtractor
    {
        public MultiQueryDataSourceChangeParam RecalculateMemberMapping(LambdaExpression lambdaExpr)
        {
            var newExpr = lambdaExpr.Body as NewExpression
                            ??
                            throw new InvalidOperationException($"Join Query Method's 2nd argument must be a NewExpression.");
            var newExprMember = newExpr.Members
                                ??
                                throw new InvalidOperationException($"Join Query Method's 2nd argument must be a NewExpression with Members.");

            MemberInfo newDataSource = null;
            MemberInfo oldDataSource = null;
            var newMap = new Dictionary<MemberInfo, MemberInfo>();
            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var member = newExprMember[i];
                var expr = newExpr.Arguments[i];
                if (expr == lambdaExpr.Parameters[1])
                {
                    newDataSource = member;
                    continue;
                }
                if (expr == lambdaExpr.Parameters[0])
                {
                    oldDataSource = member;
                    continue;
                }
                if (expr is MemberExpression)
                {
                    var dataSourceMemberExpr = expr as MemberExpression
                        ??
                        throw new InvalidOperationException($"2nd Argument of Join Query Method is a NewExpression but it's argument {i} is not a MemberExpression.");
                    var oldPropertyInfo = dataSourceMemberExpr.Member;
                    newMap.Add(member, oldPropertyInfo);
                }
            }
            // now we are using this class in SelectMany, where this is possible that newDataSource is not selected in the 
            // Selector part
            //if (newDataSource == null)
            //    throw new InvalidOperationException($"Unable to find the new data source in the 2nd argument of Join Query Method, make sure you have selected the 2nd parameter in New Data Source Expression.");

            // oldDataSource will be null in-case if the sub-expressions were selected
            // e.g.
            // QueryExtensions.From<Student>()
            //  .Join(QueryExtensions.From<StudentGrade>(), (s, sg) => new { s, sg }, x => x.s.StudentId == x.sg.StudentId)
            //  .Join(QueryExtensions.From<StudentGradeDetail>(), (o, sgd) => new { o.s, o.sg, sgd }, x => x.sg.StudentGradeId == x.sgd.StudentGradeId)
            // In above example, first Join will have oldDataSource as not null, because "s" is directly selected in new
            // while 2nd Join will have oldDataSource as null, because "o" is not directly selected in new, rather properties of "o" are selected

            var changeDataSourceParam = new MultiQueryDataSourceChangeParam(oldDataSource, newDataSource, newMap);
            return changeDataSourceParam;
        }

        //private static LambdaExpression GetLambdaExpression(Expression newDataSourceExpr)
        //{
        //    if (newDataSourceExpr is UnaryExpression quoteExpr)
        //        newDataSourceExpr = quoteExpr.Operand;
        //    var lambdaExpr = newDataSourceExpr as LambdaExpression
        //                    ??
        //                    throw new InvalidOperationException($"Join Query Method's 2nd argument must be a LambdaExpression.");
        //    return lambdaExpr;
        //}
    }



    public class MultiQueryDataSourceChangeParam
    {
        public MemberInfo CurrentDataSourceMemberInfo { get; }
        public MemberInfo NewDataSourceMemberInfo { get; }
        public Dictionary<MemberInfo, MemberInfo> NewMap { get; }

        public MultiQueryDataSourceChangeParam(MemberInfo currentDataSource, MemberInfo newDataSource, Dictionary<MemberInfo, MemberInfo> newMap)
        {
            this.CurrentDataSourceMemberInfo = currentDataSource;
            this.NewDataSourceMemberInfo = newDataSource;
            this.NewMap = newMap;
        }
    }
}
