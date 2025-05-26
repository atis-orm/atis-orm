using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine.ExpressionExtensions
{
    /*
     
            db.Table1.Where(x => x.Field3 == 1).Where(x => db.Table2.Where(y => y.Field1 == x.NavProp1().Field2 && y.Field2 == x.NavProp1().NavProp2().Field3).Any());

            1. NavigationExpression(x, "NavProp1", LeftJoin)                                                            x.NavProp1()
            2. NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin)                x.NavProp1().NavProp2()
            3. NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin).Field3         x.NavProp1().NavProp2().Field3

            1. x Visited
            2. x.NavProp1() Visited
                    Received x.NavProp1()
                    Navigation = true, returns NavigationExpression(x, "NavProp1", LeftJoin)
            3. x.NavProp1().NavProp2() Visited  
                    Received NavigationExpression(x, "NavProp1", LeftJoin).NavProp1()
                    Navigation = true, returns NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin)
            4. x.NavProp1().NavProp2().Field3 Visited   
                    Received NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin).Field3
                    Navigation = false, returns NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin).Field3

    Example-2: x.NavProp1().NavProp2().NavProp3().Field4
            1. x Visited
            2. x.NavProp1() Visited
                    Received x.NavProp1()
                    Navigation = true, returns NavigationExpression(x, "NavProp1", LeftJoin)
            3. x.NavProp1().NavProp2() Visited  
                    Received NavigationExpression(x, "NavProp1", LeftJoin).NavProp1()
                    Navigation = true, returns NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin)
            4. x.NavProp1().NavProp2().NavProp3() Visited
                    Received NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin).NavProp3()
                    Navigation = true, returns NavigationExpression(NavigationExpression(NavigationExpression(x, "NavProp1", LeftJoin), "NavProp2", LeftJoin), "NavProp3", LeftJoin)


    Original:
        Where(db.Table1, wherePredicate)

    Wrapping:
        1. Where(NavigationJoin(db.Table1, t1 => db.Table2, (t1, t2) => t1.PK == t2.FK, joinType, "NavProp1"), wherePredicate)
        2. Where(NavigationJoin(NavigationJoin(db.Table1, t1 => db.Table2, (t1, t2) => t1.PK == t2.FK, joinType, "NavProp1"), t2 => db.Table3, (t2, t3) => t2.PK == t3.FK, joinType, "NavProp2"), wherePredicate)
            
     
     */

    public class NavigationJoinCallExpression : Expression
    {
        public NavigationJoinCallExpression(Expression querySource, LambdaExpression parentSelection, string navigationProperty, LambdaExpression joinedDataSource, LambdaExpression joinCondition, SqlJoinType sqlJoinType, NavigationType navigationType)
        {
            this.QuerySource = querySource ?? throw new ArgumentNullException(nameof(querySource));
            this.ParentSelection = parentSelection ?? throw new ArgumentNullException(nameof(parentSelection));
            this.NavigationProperty = navigationProperty ?? throw new ArgumentNullException(nameof(navigationProperty));
            this.JoinedDataSource = joinedDataSource ?? throw new ArgumentNullException(nameof(joinedDataSource));
            this.JoinCondition = joinCondition;
            this.SqlJoinType = sqlJoinType;
            this.NavigationType = navigationType;
            this.Type = querySource.Type;
        }

        /// <inheritdoc />
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        ///     <para>
        ///         Gets the source expression.
        ///     </para>
        /// </summary>
        public Expression QuerySource { get; }
        public LambdaExpression ParentSelection { get; }

        /// <summary>
        ///     <para>
        ///         Gets the navigation property name.
        ///     </para>
        /// </summary>
        public string NavigationProperty { get; }

        /// <summary>
        ///     <para>
        ///         Gets the joined data source expression.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Should be like this <c>parent => tableSource</c>
        ///     </para>
        /// </remarks>
        public LambdaExpression JoinedDataSource { get; }

        /// <summary>
        ///     <para>
        ///         Gets the join condition expression.
        ///     </para>
        /// </summary>
        public LambdaExpression JoinCondition { get; }

        /// <summary>
        ///     <para>
        ///         Gets the type of join to be used.
        ///     </para>
        /// </summary>
        public SqlJoinType SqlJoinType { get; }
        public NavigationType NavigationType { get; }

        /// <inheritdoc />
        public override Type Type { get; }

        public NavigationJoinCallExpression Update(Expression querySource, LambdaExpression sourceSelection, LambdaExpression joinedDataSource, LambdaExpression joinCondition)
        {
            if (querySource == this.QuerySource && sourceSelection == this.ParentSelection && joinedDataSource == this.JoinedDataSource && joinCondition == this.JoinCondition)
                return this;
            return new NavigationJoinCallExpression(querySource, sourceSelection, this.NavigationProperty, joinedDataSource, joinCondition, this.SqlJoinType, this.NavigationType);
        }

        /// <inheritdoc />
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var querySource = visitor.Visit(this.QuerySource);
            var sourceSelection = visitor.VisitAndConvert(this.ParentSelection, $"{nameof(NavigationJoinCallExpression)}.{nameof(VisitChildren)}-1");
            var joinedDataSource = visitor.VisitAndConvert(this.JoinedDataSource, $"{nameof(NavigationJoinCallExpression)}.{nameof(VisitChildren)}-2");
            var joinCondition = visitor.VisitAndConvert(this.JoinCondition, $"{nameof(NavigationJoinCallExpression)}.{nameof(VisitChildren)}-3");
            return this.Update(querySource, sourceSelection, joinedDataSource, joinCondition);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"NavJoin({this.QuerySource}, \"{this.NavigationProperty}\")";
        }
    }
}
