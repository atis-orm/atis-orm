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
                    (
                    SupportedMethods.Contains(methodCallExpr.Method.Name)
                    ||
                    (methodCallExpr.Method.Name == nameof(string.Concat) && IsConcatMethodCall(methodCallExpr))
                    ))
            {
                converter = new StringFunctionsConverter(this.Context, methodCallExpr, converterStack);
                return true;
            }
            converter = null;
            return false;
        }

        private bool IsConcatMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(string))
                return false;

            if (methodCallExpression.Method.Name != nameof(string.Concat))
                return false;

            var parameters = methodCallExpression.Method.GetParameters();

            if (parameters.Length == 1)
            {
                var paramType = parameters[0].ParameterType;

                if (paramType == typeof(object[]))
                    return true;

                if (paramType == typeof(string[]))
                    return true;
            }

            if (parameters.Length > 1)
            {
                if (parameters.All(p => p.ParameterType == typeof(string)))
                    return true;

                if (parameters.All(p => p.ParameterType == typeof(object)))
                    return true;
            }

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

        private SqlBinaryExpression CreateSqlBinary(SqlExpression left, SqlExpression right, SqlExpressionType operation)
        {
            return this.SqlFactory.CreateBinary(left, right, operation);
        }

        private static string[] likeMethods = new[] { nameof(string.Contains), nameof(string.StartsWith), nameof(string.EndsWith) };

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            SqlExpression stringExpression = convertedChildren[0];
            SqlExpression[] arguments = convertedChildren.Skip(1).ToArray();

            if (likeMethods.Contains(this.Expression.Method.Name))
            {
                if (arguments.Length == 0)
                {
                    throw new ArgumentException($"The method '{this.Expression.Method.Name}' requires at least one argument.");
                }
                return this.CreateLikeMethodExpression(stringExpression, arguments[0]);
            }
            else
            {
                return this.CreateStringFunction(stringExpression, arguments);
            }
        }

        private SqlExpression CreateStringFunction(SqlExpression stringExpression, SqlExpression[] arguments)
        {
            SqlStringFunction stringFunction;
            switch (this.Expression.Method.Name)
            {
                case nameof(string.Substring):
                    {
                        stringFunction = SqlStringFunction.SubString;
                        break;
                    }
                case nameof(string.ToLower):
                    {
                        stringFunction = SqlStringFunction.ToLower;
                        break;
                    }
                case nameof(string.ToUpper):
                    {
                        stringFunction = SqlStringFunction.ToUpper;
                        break;
                    }
                case nameof(string.Trim):
                    {
                        stringFunction = SqlStringFunction.Trim;
                        break;
                    }
                case nameof(string.TrimEnd):
                    {
                        stringFunction = SqlStringFunction.TrimEnd;
                        break;
                    }
                case nameof(string.TrimStart):
                    {
                        stringFunction = SqlStringFunction.TrimStart;
                        break;
                    }
                case nameof(string.IndexOf):
                    {
                        stringFunction = SqlStringFunction.CharIndex;
                        break;
                    }
                case nameof(string.Replace):
                    {
                        stringFunction = SqlStringFunction.Replace;
                        break;
                    }
                case nameof(string.Concat):
                    {
                        stringFunction = SqlStringFunction.Concat;
                        break;
                    }
                default:
                    throw new InvalidOperationException($"String method '{this.Expression.Method.Name}' is not supported.");
            }

            return this.SqlFactory.CreateStringFunction(stringFunction, stringExpression, arguments);
        }

        private SqlExpression CreateLikeMethodExpression(SqlExpression stringExpression, SqlExpression pattern)
        {
            switch (this.Expression.Method.Name)
            {
                case nameof(string.Contains):
                    {
                        return this.SqlFactory.CreateLike(stringExpression, pattern);
                    }
                case nameof(string.StartsWith):
                    {
                        return this.SqlFactory.CreateLikeStartsWith(stringExpression, pattern);
                    }
                case nameof(string.EndsWith):
                    {
                        return this.SqlFactory.CreateLikeEndsWith(stringExpression, pattern);
                    }
                default:
                    throw new InvalidOperationException($"Impossible to land here");
            }
        }
    }
}
