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
    ///         Factory class for creating converters that handle string functions in LINQ to SQL expressions.
    ///     </para>
    /// </summary>
    public class StringFunctionsConverterFactory : LinqToSqlExpressionConverterFactoryBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         List of supported string methods.
        ///     </para>
        /// </summary>
        protected static readonly string[] SupportedMethods = new string[] {
                nameof(string.Substring), nameof(string.ToLower), nameof(string.ToUpper), nameof(string.Trim), nameof(string.TrimEnd), nameof(string.TrimStart),
                nameof(string.Contains), nameof(string.StartsWith), nameof(string.EndsWith), nameof(string.IndexOf), nameof(string.Replace),
            };

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="StringFunctionsConverterFactory"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        public StringFunctionsConverterFactory(IConversionContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is MethodCallExpression methodCallExpr &&
                    methodCallExpr.Method.DeclaringType == typeof(string) &&
                    SupportedMethods.Contains(methodCallExpr.Method.Name))
            {
                converter = new StringFunctionsConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Converter class for handling string functions in LINQ to SQL expressions.
    ///     </para>
    /// </summary>
    public class StringFunctionsConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="StringFunctionsConverter"/> class.
        ///     </para>
        /// </summary>
        /// <param name="context">The conversion context.</param>
        /// <param name="expression">The method call expression to be converted.</param>
        /// <param name="converterStack">The stack of converters representing the parent chain for context-aware conversion.</param>
        public StringFunctionsConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack)
            : base(context, expression, converterStack)
        {
        }

        /// <summary>
        ///     <para>
        ///         Gets the arguments for the SQL function call from the method call expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">The method call expression.</param>
        /// <param name="convertedChildren">The stack of SQL expressions.</param>
        /// <returns>An array of SQL expressions representing the arguments.</returns>
        protected virtual SqlExpression[] GetArguments(MethodCallExpression expression, SqlExpression[] convertedChildren)
        {
            var args = convertedChildren.Skip(1).ToArray();
            // convertedChildren[0] is the instance of the string object on which the method is called
            var result = new SqlExpression[] { convertedChildren[0] }.Concat(args).ToArray();
            return result;
        }

        /// <summary>
        ///     <para>
        ///         Creates the SQL expression for the given method call expression.
        ///     </para>
        /// </summary>
        /// <param name="expression">The method call expression.</param>
        /// <param name="convertedChildren">The stack of SQL expressions.</param>
        /// <returns>The SQL expression representing the method call.</returns>
        protected virtual SqlExpression CreateSql(MethodCallExpression expression, SqlExpression[] convertedChildren)
        {
            var methodName = expression.Method.Name;
            var arguments = GetArguments(expression, convertedChildren);
            switch (methodName)
            {
                case nameof(string.Substring):
                    {
                        var lastArgument = arguments.Length == 3 ? arguments[2] : this.SqlFactory.CreateLiteral(int.MaxValue);
                        var arg1 = this.CreateSqlBinary(arguments[1], this.SqlFactory.CreateLiteral(1), SqlExpressionType.Add);
                        return this.CreateSqlFunction("substring", arguments[0], arg1, lastArgument);
                    }
                case nameof(string.ToLower):
                    {
                        return this.CreateSqlFunction("lower", arguments[0]);
                    }
                case nameof(string.ToUpper):
                    {
                        return this.CreateSqlFunction("upper", arguments[0]);
                    }
                case nameof(string.Trim):
                    {
                        return this.CreateSqlFunction("rTrim", this.CreateSqlFunction("lTrim", arguments[0]));
                    }
                case nameof(string.TrimEnd):
                    {
                        return this.CreateSqlFunction("rTrim", arguments[0]);
                    }
                case nameof(string.TrimStart):
                    {
                        return this.CreateSqlFunction("lTrim", arguments[0]);
                    }
                case nameof(string.Contains):
                    {
                        return this.CreateSqlBinary(arguments[0], this.CreateSqlBinary(this.GetPercent(), this.CreateSqlBinary(arguments[1], this.GetPercent(), SqlExpressionType.Add), SqlExpressionType.Add), SqlExpressionType.Like);
                    }
                case nameof(string.StartsWith):
                    {
                        return this.CreateSqlBinary(arguments[0], this.CreateSqlBinary(arguments[1], this.GetPercent(), SqlExpressionType.Add), SqlExpressionType.Like);
                    }
                case nameof(string.EndsWith):
                    {
                        return this.CreateSqlBinary(arguments[0], this.CreateSqlBinary(this.GetPercent(), arguments[1], SqlExpressionType.Add), SqlExpressionType.Like);
                    }
                case nameof(string.IndexOf):
                    {
                        return this.CreateSqlFunction("charIndex", arguments[1], arguments[0], arguments.Skip(2).FirstOrDefault());
                    }
                case nameof(string.Replace):
                    {
                        return this.CreateSqlFunction("replace", arguments[0], arguments[1], arguments[2]);
                    }
                default:
                    throw new InvalidOperationException($"String method '{methodName}' is not supported.");
            }
        }

        private SqlFunctionCallExpression CreateSqlFunction(string functionName, params SqlExpression[] arguments)
        {
            return this.SqlFactory.CreateFunctionCall(functionName, arguments);
        }

        private SqlLiteralExpression GetPercent()
        {
            return this.SqlFactory.CreateLiteral("%");
        }

        private SqlBinaryExpression CreateSqlBinary(SqlExpression left, SqlExpression right, SqlExpressionType operation)
        {
            return this.SqlFactory.CreateBinary(left, right, operation);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            var expression = this.Expression;
            var sql = this.CreateSql(expression, convertedChildren);
            return sql;
        }
    }
}
