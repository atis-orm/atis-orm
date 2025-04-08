using System;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a SQL parameter expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define a parameter value in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlParameterExpression : SqlExpression
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlParameterExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Sets the value of the SQL parameter.
        ///     </para>
        /// </summary>
        /// <param name="value">The value of the SQL parameter.</param>
        /// <param name="multipleValues">Flag indicating if the parameter can have multiple values.</param>
        public SqlParameterExpression(object value, bool multipleValues)
        {
            this.Value = value;
            this.MultipleValues = multipleValues;
        }

        /// <summary>
        ///     <para>
        ///         Gets the type of the SQL expression node.
        ///     </para>
        ///     <para>
        ///         This property always returns <see cref="SqlExpressionType.Parameter"/>.
        ///     </para>
        /// </summary>
        public override SqlExpressionType NodeType => SqlExpressionType.Parameter;

        /// <summary>
        ///     <para>
        ///         Gets the value of the SQL parameter.
        ///     </para>
        /// </summary>
        public object Value { get; }
        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether the parameter can have multiple values.
        ///     </para>
        /// </summary>
        public bool MultipleValues { get; }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the SQL parameter expression.
        ///     </para>
        ///     <para>
        ///         The string representation includes the parameter value.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the SQL parameter expression.</returns>
        public override string ToString()
        {
            return ConvertObjectToString(this.Value);
        }

        /// <summary>
        ///     <para>
        ///         Converts an object to its string representation for use in SQL queries.
        ///     </para>
        ///     <para>
        ///         The string representation is formatted based on the type of the object.
        ///     </para>
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>A string representation of the object.</returns>
        /// <remarks>
        ///     <para>
        ///         This is just a simple method provided for shortcut, do not rely on this method.
        ///     </para>
        /// </remarks>
        public static string ConvertObjectToString(object value)
        {
            string strValue;
            bool encloseInQuotes = value is string || value is Guid || value is DateTime;
            if (value == null)
            {
                strValue = "null";
            }
            else if (value is DateTime dt)
            {
                strValue = $"{dt:yyyy-MM-dd HH:mm:ss}";
            }
            else if (!(value is string) && value is System.Collections.IEnumerable values)
            {
                var valuesToString = new List<string>();
                foreach (var val in values)
                {
                    valuesToString.Add(ConvertObjectToString(val));
                }
                return string.Join(",", valuesToString);
            }
            else if (value is Boolean b)
            {
                return b ? "1" : "0";
            }
            else
                strValue = $"{value}";

            strValue = strValue.Replace("'", "''");
            if (encloseInQuotes)
            {
                strValue = $"'{strValue}'";
            }
            return strValue;
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL parameter expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlParameterExpression(this);
        }
    }
}
