using Atis.LinqToSql.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.LinqToSql
{
    /// <summary>
    ///     <para>
    ///         
    ///     </para>
    /// </summary>
    public class ExpressionHashGenerator : ExpressionVisitor
    {
        private HashCode hashCode;
        private readonly IReflectionService reflectionService;

        /// <summary>
        ///     <para>
        ///         Creates a new instance of <see cref="ExpressionHashGenerator"/> class.
        ///     </para>
        /// </summary>
        /// <param name="reflectionService">Reflection service.</param>
        public ExpressionHashGenerator(IReflectionService reflectionService)
        {
            this.reflectionService = reflectionService;
        }

        /// <summary>
        ///     <para>
        ///         Generates hash code for the given expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">Expression to generate hash code.</param>
        public int GenerateHash(Expression expression)
        {
            this.hashCode = new HashCode();
            Visit(expression);
            return hashCode.ToHashCode();
        }

        /// <inheritdoc />
        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                hashCode.Add(0);
            }
            else
            {
                hashCode.Add(node.NodeType);
                hashCode.Add(node.Type);
            }
            return base.Visit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            // we don't want to add value in HashCode if the value itself is a Query constant
            // e.g.  IQueryable<SomeType> query = context.SomeType;
            // in above query.Expression will be ConstantExpression and the value will be pointing
            // back the the same 'query' instance.
            // Therefore,IQueryable<SomeType> q1 = context.SomeType and IQueryable<SomeType> q2 = context.SomeType
            // should have same hash code.
            if (node.Value != null && !this.reflectionService.IsQueryableType(node.Value.GetType()))
            {
                this.hashCode.Add(node.Value);
            }
            return base.VisitConstant(node);
        }

        /// <summary>
        ///     <para>
        ///         This method is used to get the constant value from the MemberExpression.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Even if the constant value is null, we still want to return true in the first item of the tuple.
        ///         So that the caller will know that the MemberExpression is pointing to a constant value.
        ///     </para>
        /// </remarks>
        /// <param name="memberExpression">MemberExpression to get the constant value.</param>
        /// <returns>
        ///     Item-1: True if the value is constant, otherwise false.
        ///     Item-2: The constant value.
        /// </returns>
        private (bool, object) GetConstantValueFromMemberExpression(MemberExpression memberExpression)
        {
            bool wasConstant = false;
            object parentValue = null;
            if (memberExpression.Expression is ConstantExpression constantExpression)
            {
                parentValue = constantExpression.Value;
                wasConstant = true;
            }
            else if (memberExpression.Expression is MemberExpression parentMemberExpr)
            {
                (wasConstant, parentValue) = GetConstantValueFromMemberExpression(parentMemberExpr);
            }
            if (parentValue != null)
            {
                return (wasConstant, this.reflectionService.GetPropertyOrFieldValue(instance: parentValue, propertyOrField: memberExpression.Member));
            }
            // NOTE: do NOT try to remove the 'wasConstant' variable, you might think we can return 'false' in below
            // return and 'true' in above return, but it will be wrong. Because parentValue can be null
            // if the memberExpression.Expression is a ConstantExpression and it's Value is null. Therefore, if we
            // returned 'false' in below return then the caller will think that the MemberExpression is not pointing
            // to a constant value, which is wrong.
            return (wasConstant, null);
        }


        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            var propertyInfoOrFieldInfoType = (node.Member as PropertyInfo)?.PropertyType ?? (node.Member as FieldInfo)?.FieldType;
            // this is highly unlikely that propertyInfoOrFieldInfoType is null, but we are checking it
            if (propertyInfoOrFieldInfoType != null)
            {
                var (isConstant, constantValue) = this.GetConstantValueFromMemberExpression(node);
                // if constantValue is not null it means MemberExpression is pointing to a constant value
                // in this case we must return the same hash code regardless of the value, since the shape of
                // the expression will be same. For Example,
                //      int intVar = 5;
                //      var q1 = context.SomeType.Where(x => x.Id == intVar);
                //      intVar = 10;
                //      var q2 = context.SomeType.Where(y => x.Id == intVar);
                // q1 and q2 should have same hash code.
                if (isConstant)
                {
                    // If constantValue is a Query instance then we need to generate hash code for the query expression.
                    // It's important to generate hash code for the query expression, otherwise, the two expressions
                    // might have same hash code even if the query expressions are different. For example,
                    //      var subQuery = context.ChildTable;
                    //      var q1 = context.ParentTable.Where(x => x.Id == subQuery.Any(y => y.FK == x.PK)).ToList();
                    //      subQuery = subQuery.Where(y => y.Name == "SomeName");
                    //      var q2 = context.ParentTable.Where(x => x.Id == subQuery.Any(y => y.FK == x.PK)).ToList();
                    // q1 and q2 should have different hash code, although, their shape is same.
                    // If we don't look at subQuery.Expression in each translation then q1 and q2 will have same hash code.
                    if (constantValue != null && this.reflectionService.IsQueryableType(constantValue.GetType()))
                    {
                        var queryExpression = this.reflectionService.GetQueryExpressionFromQueryable(constantValue);
                        var hashCodeGenerator = new ExpressionHashGenerator(this.reflectionService);
                        var subQueryHashCode = hashCodeGenerator.GenerateHash(queryExpression);
                        hashCode.Add(subQueryHashCode);
                    }
                    else
                    {
                        // If we are here it means constantValue is not a Query instance, therefore, we want the 2 expressions
                        // to have same hash code if the variables has same types. For example,
                        //      var q1 = context.SomeType.Where(x => x.Id == someClass.intVar1).ToList();
                        //      var q2 = context.SomeType.Where(y => x.Id == otherClass.intVar2).ToList();
                        //  q1 and q2 should have same hash code.
                        // IMPORTANT: we are NOT adding adding node.Member.Name in hash code, because we want the 2 expressions
                        // to have same hash code if the variable has same type. As mentioned in the example above.
                        this.hashCode.Add(propertyInfoOrFieldInfoType);
                    }
                    return node;
                }
            }

            hashCode.Add(node.Member.Name);

            return base.VisitMember(node);
        }

        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression node)
        {
            this.hashCode.Add(node.Method);
            return base.VisitUnary(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            this.hashCode.Add(node.Method);
            return base.VisitMethodCall(node);
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            this.hashCode.Add(node.ReturnType);
            return base.VisitLambda(node);
        }

        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression node)
        {
            this.hashCode.Add(node.Constructor);
            return base.VisitNew(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            foreach(var binding in node.Bindings)
            {
                this.hashCode.Add(binding.Member);
                this.hashCode.Add(binding.BindingType);
            }
            return base.VisitMemberInit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitDefault(DefaultExpression node)
        {
            this.hashCode.Add(node.Type);
            return base.VisitDefault(node);
        }

        /// <inheritdoc />
        protected override Expression VisitIndex(IndexExpression node)
        {
            this.hashCode.Add(node.Indexer);
            return base.VisitIndex(node);
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // IQueryable<SomeType> q1 = context.SomeType.Where(x => x.Id == 5);
            // IQueryable<SomeType> q2 = context.SomeType.Where(y => y.Id == 5);
            // we want to both queries to have different hash code, that's why we are
            // adding parameter name in hash code.
            this.hashCode.Add(node.Name);
            return base.VisitParameter(node);
        }
    }
}
