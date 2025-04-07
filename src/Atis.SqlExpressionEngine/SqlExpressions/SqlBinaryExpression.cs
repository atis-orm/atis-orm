using System;
using System.Collections.Generic;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a binary SQL expression.
    ///     </para>
    ///     <para>
    ///         This class is used to define binary operations in SQL queries.
    ///     </para>
    /// </summary>
    public class SqlBinaryExpression : SqlExpression
    {
        private static readonly ISet<SqlExpressionType> _allowedTypes = new HashSet<SqlExpressionType>
            {
                SqlExpressionType.Add,
                SqlExpressionType.Subtract,
                SqlExpressionType.Multiply,
                SqlExpressionType.Divide,
                SqlExpressionType.AndAlso,
                SqlExpressionType.OrElse,
                SqlExpressionType.LessThan,
                SqlExpressionType.LessThanOrEqual,
                SqlExpressionType.GreaterThan,
                SqlExpressionType.GreaterThanOrEqual,
                SqlExpressionType.Equal,
                SqlExpressionType.NotEqual,
                SqlExpressionType.Like,
                SqlExpressionType.Coalesce
            };
        private static SqlExpressionType ValidateNodeType(SqlExpressionType nodeType)
            => _allowedTypes.Contains(nodeType)
                ? nodeType
                : throw new InvalidOperationException($"SqlExpressionType '{nodeType}' is not a valid Binary Operator");

        /// <summary>
        ///     <para>  
        ///         Gets the left operand of the binary operation.
        ///     </para>
        /// </summary>
        public SqlExpression Left { get; }
        /// <summary>
        ///     <para>
        ///         Gets the right operand of the binary operation.
        ///     </para>
        /// </summary>
        public SqlExpression Right { get; }

        /// <inheritdoc />
        public override SqlExpressionType NodeType { get; }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="SqlBinaryExpression"/> class.
        ///     </para>
        ///     <para>
        ///         Validates the node type and sets the left and right operands.
        ///     </para>
        /// </summary>
        /// <param name="left">The left operand of the binary operation.</param>
        /// <param name="right">The right operand of the binary operation.</param>
        /// <param name="sqlExpressionType">The type of the binary operation.</param>
        public SqlBinaryExpression(SqlExpression left, SqlExpression right, SqlExpressionType sqlExpressionType)
        {
            this.Left = left ?? throw new ArgumentNullException(nameof(left));
            this.Right = right ?? throw new ArgumentNullException(nameof(right));
            this.NodeType = ValidateNodeType(sqlExpressionType);
        }

        /// <summary>
        ///     <para>
        ///         Returns a string representation of the binary expression.
        ///     </para>
        /// </summary>
        /// <returns>A string representation of the binary expression.</returns>
        public override string ToString()
        {
            return $"({this.Left?.ToString()} {GetOperator(this.NodeType)} {this.Right?.ToString()})";
        }

        private static string GetOperator(SqlExpressionType exprType)
        {
            switch (exprType)
            {
                case SqlExpressionType.Add:
                    return "+";
                case SqlExpressionType.Subtract:
                    return "-";
                case SqlExpressionType.Multiply:
                    return "*";
                case SqlExpressionType.Divide:
                    return "/";
                case SqlExpressionType.Modulus:
                    return "%";
                case SqlExpressionType.Equal:
                    return "=";
                case SqlExpressionType.NotEqual:
                    return "<>";
                case SqlExpressionType.GreaterThan:
                    return ">";
                case SqlExpressionType.GreaterThanOrEqual:
                    return ">=";
                case SqlExpressionType.LessThan:
                    return "<";
                case SqlExpressionType.LessThanOrEqual:
                    return "<=";
                case SqlExpressionType.AndAlso:
                    return "and";
                case SqlExpressionType.OrElse:
                    return "or";
                case SqlExpressionType.Like:
                    return "like";
                case SqlExpressionType.Coalesce:
                    return "??";
                default:
                    return "<opr>";
            }
        }

        /// <summary>
        ///     <para>
        ///         Updates the binary expression with new operands and node type.
        ///     </para>
        /// </summary>
        /// <param name="left">The new left operand.</param>
        /// <param name="right">The new right operand.</param>
        /// <param name="nodeType">The new node type.</param>
        /// <returns>A new <see cref="SqlBinaryExpression"/> instance with the updated operands and node type, or the current instance if unchanged.</returns>
        public SqlBinaryExpression Update(SqlExpression left, SqlExpression right, SqlExpressionType nodeType)
        {
            if (left == this.Left && right == this.Right && nodeType == this.NodeType)
                return this;
            return new SqlBinaryExpression(left, right, nodeType);
        }

        /// <summary>
        ///     <para>
        ///         Accepts a visitor to visit this SQL binary expression.
        ///     </para>
        /// </summary>
        /// <param name="sqlExpressionVisitor">The visitor to accept.</param>
        /// <returns>The result of visiting this expression.</returns>
        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlBinaryExpression(this);
        }
    }
}
