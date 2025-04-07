using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL function call expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define a function call in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlFunctionCallExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlFunctionCallExpression"/> class.
        ///     </para>
        ///     <para>
        ///         The <paramref name="functionName"/> parameter specifies the name of the SQL function.
        ///     </para>
        ///     <para>
        ///         The <paramref name="arguments"/> parameter specifies the arguments for the SQL function.
        ///     </para>
        /// </summary>
        /// <param name="functionName">The name of the SQL function.</param>
        /// <param name="arguments">The arguments for the SQL function.</param>
        public SqlFunctionCallExpression(string functionName, params SqlExpression[] arguments)
            : this(functionName, arguments.AsEnumerable())
        {
        }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlFunctionCallExpression"/> class.
        ///     </para>
        ///     <para>
        ///         The <paramref name="functionName"/> parameter specifies the name of the SQL function.
        ///     </para>
        ///     <para>
        ///         The <paramref name="arguments"/> parameter specifies the arguments for the SQL function.
        ///     </para>
        /// </summary>
        /// <param name="functionName">The name of the SQL function.</param>
        /// <param name="arguments">The arguments for the SQL function.</param>
        public SqlFunctionCallExpression(string functionName, IEnumerable<SqlExpression> arguments)
        {
            this.FunctionName = functionName;
            this.Arguments = arguments;
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.FunctionCall"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.FunctionCall;

        /// <summary>
        ///     <para>
        ///         Gets the name of the SQL function.
        ///     </para>
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        ///     <para>
        ///         Gets the arguments for the SQL function.
        ///     </para>
        /// </summary>
        public IEnumerable<SqlExpression> Arguments { get; }

        
        public SqlFunctionCallExpression Update(IEnumerable<SqlExpression> arguments)
        {
            if (((this.Arguments?.Any() ?? false) == false) && ((arguments?.Any() ?? false) == false))
                return this;

            if (arguments == this.Arguments || 
                    (arguments != null && this.Arguments != null &&
                    arguments.SequenceEqual(this.Arguments)))
                return this;

            return new SqlFunctionCallExpression(this.FunctionName, arguments);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL function call expression.
        ///     </para>
        ///     <para>
        ///         This method is used to implement the visitor pattern for SQL expressions.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlFunctionCallExpression(this);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL function call expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the function name and its arguments.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL function call expression.</returns>
        public override string ToString()
        {
            var arguments = this.Arguments != null ? string.Join(", ", this.Arguments.Select(a => a.ToString())) : string.Empty;
            return $"{this.FunctionName}({arguments})";
        }
    }
}
