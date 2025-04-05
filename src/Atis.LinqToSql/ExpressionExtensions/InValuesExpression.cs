using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.LinqToSql.ExpressionExtensions
{
    /// <summary>
    ///     <para>
    ///         Represents an expression that checks if a given expression is in a list of values.
    ///     </para>
    /// </summary>
    public class InValuesExpression : Expression
    {
        /// <summary>
        ///     <para>
        ///         Creates a new instance of the <see cref="InValuesExpression"/> class.
        ///     </para>
        /// </summary>
        /// <param name="expression">Expression to be checked for membership in the list of values.</param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="expression"/> is null.</exception>
        public InValuesExpression(Expression expression, Expression values)
        {
            this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            // We are taking values as single Expression instead of a collection of values
            // because this is possible that user have used a variable which might have different
            // values on run time, for example,
            //          var q = employees.Where(e => e.Department.Contains(departmentList));
            // and departmentList is a variable which might have different values on run time.
            // Let's assume that this query is being written in a method where departmentList is
            // coming from UI. First time when the departmentList came, it had 3 values,
            // and at that time the query was not cached, so, in that case the whole cycle will
            // be executed and the SqlQueryExpression will be created and there we'll have a
            // SqlInValuesExpression with 3 values. After this the SqlQueryExpression will be
            // translated to SQL String and it will be cached along with 3 parameters.
            this.Values = values;
        }

        /// <inheritdoc />
        public override ExpressionType NodeType => ExpressionType.Extension;
        /// <summary>
        ///     <para>
        ///         Gets the expression to be checked for membership in the list of values.
        ///     </para>
        /// </summary>
        public Expression Expression { get; }
        /// <summary>
        ///     <para>
        ///         
        ///     </para>
        /// </summary>
        public Expression Values { get; }
        /// <inheritdoc />
        public sealed override Type Type => typeof(bool);

        /// <inheritdoc />
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var updatedExpression = visitor.Visit(Expression);
            var updatedValues = visitor.Visit(Values);

            if (updatedExpression == Expression && updatedValues == Values)
            {
                return this;
            }
            return new InValuesExpression(updatedExpression, updatedValues);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.GetType().Name}({this.Expression}, {this.Values})";
        }
    }
}
