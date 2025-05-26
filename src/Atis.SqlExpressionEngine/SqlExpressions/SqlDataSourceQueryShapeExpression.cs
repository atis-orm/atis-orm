using Atis.SqlExpressionEngine.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlMemberAssignment
    {
        public SqlMemberAssignment(string memberName, SqlExpression sqlExpression)
            : this(memberName, sqlExpression, projectable: true)
        { }

        public SqlMemberAssignment(string memberName, SqlExpression sqlExpression, bool projectable)
        {
            this.MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            this.SqlExpression = sqlExpression ?? throw new ArgumentNullException(nameof(sqlExpression));
            this.Projectable = projectable;
        }

        public string MemberName { get; }
        public SqlExpression SqlExpression { get; }
        public bool Projectable { get; }

        public override string ToString()
        {
            return $"{MemberName} = {SqlExpression}{(!Projectable ? "*" : "")}";
        }
    }

    public abstract class SqlQueryShapeExpression : SqlExpression
    {
        public abstract bool TryResolveMember(string memberName, out SqlExpression resolvedExpression);
        public abstract void AddMemberAssignment(string memberName, SqlExpression assignment, bool projectable);
        public abstract void RemoveMember(string memberName);
        public abstract bool IsScalar { get; }
        public abstract SqlExpression GetScalarExpression();
    }

    public class SqlDataSourceQueryShapeExpression : SqlQueryShapeExpression
    {
        public SqlDataSourceQueryShapeExpression(SqlExpression shapeExpression, Guid dataSourceAlias)
        {
            // in SqlDataSourceQueryShapeExpression either we will have a SqlMemberInitExpression or some other SqlExpression
            // but we cannot have SqlDataSourceQueryShapeExpression nested within SqlDataSourceQueryShapeExpression
            if (shapeExpression is SqlDataSourceQueryShapeExpression)
                throw new ArgumentException("Cannot create SqlDataSourceQueryShapeExpression from another SqlDataSourceQueryShapeExpression", nameof(shapeExpression));
            this.ShapeExpression = shapeExpression ?? throw new ArgumentNullException(nameof(shapeExpression));
            this.DataSourceAlias = dataSourceAlias;
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.DataSourceQueryShape;
        public SqlExpression ShapeExpression { get; }
        public Guid DataSourceAlias { get; }
        public override bool IsScalar => !(this.ShapeExpression is SqlQueryShapeExpression);
        public override SqlExpression GetScalarExpression() => this.IsScalar ? this.ShapeExpression : throw new InvalidOperationException("Not a scalar expression.");

        public override bool TryResolveMember(string memberName, out SqlExpression resolvedExpression)
        {
            if (memberName is null)
                throw new ArgumentNullException(nameof(memberName));
            if (this.ShapeExpression is SqlMemberInitExpression memberInit)
                return memberInit.TryResolveMember(memberName, out resolvedExpression);
            // in-case if ShapeExpression is a scalar column we'll return as is
            resolvedExpression = this.ShapeExpression;
            return true;
        }

        public override void AddMemberAssignment(string memberName, SqlExpression assignment, bool projectable)
        {
            if (this.ShapeExpression is SqlMemberInitExpression memberInit)
            {
                memberInit.AddMemberAssignment(memberName, assignment, projectable);
            }
            else
            {
                throw new InvalidOperationException("Cannot add member assignment to non-member init expression.");
            }
        }

        public override void RemoveMember(string memberName)
        {
            if (this.ShapeExpression is SqlMemberInitExpression memberInit)
            {
                memberInit.RemoveMember(memberName);
            }
            else
            {
                throw new InvalidOperationException("Cannot remove member from non-member init expression.");
            }
        }

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitDataSourceQueryShape(this);
        }

        public SqlDataSourceQueryShapeExpression Update(SqlExpression shapeExpression)
        {
            if (shapeExpression == this.ShapeExpression)
                return this;
            return new SqlDataSourceQueryShapeExpression(shapeExpression, this.DataSourceAlias);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"ds-shape: {DebugAliasGenerator.GetAlias(this.DataSourceAlias)}";
        }
    }

    public class SqlQueryShapeFieldResolverExpression : SqlQueryShapeExpression
    {
        public SqlQueryShapeFieldResolverExpression(SqlQueryShapeExpression queryShape, SqlSelectExpression selectQuery)
        {
            if (queryShape is SqlQueryShapeFieldResolverExpression)
                throw new ArgumentException("Cannot create SqlQueryShapeMemberExpression from another SqlQueryShapeMemberExpression", nameof(queryShape));
            this.ShapeExpression = queryShape ?? throw new ArgumentNullException(nameof(queryShape));
            this.SelectQuery = selectQuery ?? throw new ArgumentNullException(nameof(selectQuery));
        }
        public override SqlExpressionType NodeType => SqlExpressionType.QueryShapeMember;
        public override bool IsScalar => this.ShapeExpression.IsScalar;
        public override SqlExpression GetScalarExpression() => this.ShapeExpression.GetScalarExpression();

        public SqlQueryShapeExpression ShapeExpression { get; }
        public SqlSelectExpression SelectQuery { get; }

        public override void AddMemberAssignment(string memberName, SqlExpression assignment, bool projectable)
        {
            // since this class works as a wrapper and should never be used for actual modification
            // that's why we are not implementing the methods
            throw new NotImplementedException();
        }

        public override void RemoveMember(string memberName)
        {
            // since this class works as a wrapper and should never be used for actual modification
            // that's why we are not implementing the methods
            throw new NotImplementedException();
        }

        public override bool TryResolveMember(string memberName, out SqlExpression resolvedExpression)
        {
            if (this.ShapeExpression.TryResolveMember(memberName, out var assignment))
            {
                if (assignment is SqlQueryShapeExpression qs)
                {
                    if (qs.IsScalar)
                        resolvedExpression = qs.GetScalarExpression();
                    else
                        resolvedExpression = new SqlQueryShapeFieldResolverExpression(qs, this.SelectQuery);
                }
                else
                    resolvedExpression = assignment;
                // here we are testing if the SelectQuery has not projection applied
                // which is to avoid the case if a sub-query has been selected in projection we cannot 
                // convert it to outer apply
                if (!this.SelectQuery.HasProjectionApplied && resolvedExpression is SqlDerivedTableExpression derivedTable)
                {
                    // Although we are here adding the outer apply and projections of outer apply in
                    // the query shape, but we are NOT really sure if it's a good idea. We are basically
                    // assuming that the resolve part will always be done from MemberExpressionConverter
                    // and it will be done on the projection level or at-least on the point where query
                    // shape is being changed.
                    var navigationParent = this.ShapeExpression;
                    // since current memberName is being mapped to derivedTable so we are removing it and we'll
                    // add it's columns in current query-shape
                    navigationParent.RemoveMember(memberName);
                    var dsQueryShape = this.SelectQuery.AddNavigationJoin(navigationParent, derivedTable, SqlJoinType.OuterApply, memberName);
                    if (dsQueryShape is SqlQueryShapeExpression d)
                        resolvedExpression = new SqlQueryShapeFieldResolverExpression(d, this.SelectQuery);
                    else
                        resolvedExpression = dsQueryShape;
                }
                return true;
            }
            else
            {
                resolvedExpression = null;
                return false;
            }
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            throw new NotSupportedException("Does not support visit");
        }
    }

    /*
    
    E.g.:

    Initial From method input => new { t1 = db.Table1, t2 = db.Table2 }

    Query Shape:

    SqlMemberInitExpression
        Bindings = [
                    t1 = SqlQueryShapeExpression { DataSourceAlias = t1, ShapeExpression = SqlMemberInitExpression { Bindings = [Field1 = a_1.Field1, Field2 = a_2.Field2] } },
                    t2 = SqlQueryShapeExpression { DataSourceAlias = t2, ShapeExpression = SqlMemberInitExpression { Bindings = [Field1 = a_1.Field1, Field2 = a_2.Field2] } }
                    ]

    Resolving:  x.t1.Field1
        x               => SqlSelectExpression.QueryShape which is SqlMemberInitExpression
        x.t1            => We'll receive SqlMemberInitExpression after converting `x`
                            Now MemberExpressionConverter will check it's a SqlMemberInitExpression and find
                            `t1` in the bindings and will return the ShapeExpression which is SqlMemberInitExpression
        x.t1.Field1     => We'll receive SqlMemberInitExpression after converting `x.t1`
                            Now MemberExpressionConverter will check it's a SqlMemberInitExpression and find
                            `Field1` in the bindings and will return the SqlDataSourceColumnExpression which is a field
                            of the table.

    -----

    Initial Query Making input => db.Table1 (single table)

    Query Shape:

    SqlQueryShapeExpression { DataSourceAlias = a_1, ShapeExpression = SqlMemberInitExpression { Bindings = [Field1 = a_1.Field1, Field2 = a_2.Field2] } }

    Resolving:  x.Field1
        x               => SqlSelectExpression.QueryShape which is SqlQueryShapeExpression
        x.Field1        => We'll receive SqlQueryShapeExpression after converting `x`
                            Now MemberExpressionConverter will check it's a SqlQueryShapeExpression and find
                            `Field1` in the bindings and will return the SqlDataSourceColumnExpression which is a field
                            of the table.

    -----

    Findings: we can see that MemberExpressionConverter will either receive SqlMemberInitExpression or SqlQueryShapeExpression
    
    ---- projection --- 

    .Select(x => new { Table1 = x.t1 } )

    In above NewExpression will be converted to SqlMemberInitExpression, so first Member is `Table1` and
    `x.t1` will be resolved to SqlQueryShapeExpression and will be assigned as is

    ------

    .From(() => new { t1 = db.Table1.GroupBy(p1 => p1.Field1).Select(p2 => p2.Key).Schema(),
                        t2 = db.Table2.GroupBy(p3 => p3.Field1).Select(p4 => p4.Key).Schema() })
    .LeftJoin(p5 => p5.t2, p6 => p6.t1 == p6.t2)

    Problem:
        `p5.t2` should be resolved to `SqlDataSourceQueryShapeExpression`
        `p6.t2` should be resolved to `SqlDataSourceColumnExpression`

     * */
}
