using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.ExpressionConverters
{

    public class StringCompareToConverterFactory : LinqToSqlExpressionConverterFactoryBase<BinaryExpression>
    {
        public StringCompareToConverterFactory(IConversionContext context) : base(context)
        {
        }

        public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
        {
            if (expression is BinaryExpression binaryExpression &&
                (binaryExpression.NodeType == ExpressionType.GreaterThan ||
                binaryExpression.NodeType == ExpressionType.GreaterThanOrEqual ||
                binaryExpression.NodeType == ExpressionType.LessThan ||
                binaryExpression.NodeType == ExpressionType.LessThanOrEqual ||
                binaryExpression.NodeType == ExpressionType.Equal ||
                binaryExpression.NodeType == ExpressionType.NotEqual) &&
                binaryExpression.Left is MethodCallExpression methodCallExpression &&
                (methodCallExpression.Method.Name == nameof(string.CompareTo) ||
                methodCallExpression.Method.Name == nameof(string.Compare))
                &&
                methodCallExpression.Method.DeclaringType == typeof(string) &&
                binaryExpression.Right is ConstantExpression constantExpression &&
                (constantExpression.Value?.Equals(0) == true || constantExpression.Value?.Equals(1) == true))
            {
                converter = new StringCompareToConverter(Context, binaryExpression, converterStack);
                return true;
            }
            converter = null;
            return false;
        }
    }

    public class StringCompareToConverter : LinqToSqlExpressionConverterBase<BinaryExpression>
    {
        public StringCompareToConverter(IConversionContext context, BinaryExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
        {
        }

        /// <inheritdoc />
        public override bool TryCreateChildConverter(Expression childNode, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> childConverter)
        {
            if (childNode == this.Expression.Left && childNode is MethodCallExpression stringCompareMethodCall)
            {
                childConverter = new StringCompareMethodConverter(Context, stringCompareMethodCall, converterStack);
                return true;
            }
            return base.TryCreateChildConverter(childNode, converterStack, out childConverter);
        }

        /// <inheritdoc />
        public override SqlExpression Convert(SqlExpression[] convertedChildren)
        {
            // left > right
            // left = method call expression
            // right = constant expression
            if (convertedChildren.Length < 2)
                throw new InvalidOperationException("String comparison was not correct, should have at-least 2 conversions.");

            var collection = convertedChildren[0] as SqlCollectionExpression
                                ?? 
                                throw new InvalidOperationException("String comparison was not correct, left hand side should be a collection of expressions.");
            if (!(collection.SqlExpressions?.Count() >= 2))
                throw new InvalidOperationException("Left-hand side must have at least two string expressions.");

            var str1 = collection.SqlExpressions.First();
            var str2 = collection.SqlExpressions.ElementAt(1);
            var constantValue = (convertedChildren[1] as SqlLiteralExpression).LiteralValue as int?
                                ??
                                throw new InvalidOperationException("String comparison was not correct, right hand side should be a literal int value.");

            if (constantValue < 0 || constantValue > 1)
                throw new InvalidOperationException("String comparison was not correct, right hand side should be a literal int value of 0 or 1.");

            SqlExpressionType binaryNodeType;
            switch (this.Expression.NodeType)
            {
                case ExpressionType.GreaterThan:
                    binaryNodeType = SqlExpressionType.GreaterThan;
                    // > 0 or > 1 both should be treated as greater than
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    // string.Compare(str1, str2) >= 1          (str1 > str1)
                    // string.Compare(str1, str2) >= 0          (str1 >= str1)
                    if (constantValue == 1)
                        binaryNodeType = SqlExpressionType.GreaterThan;
                    else // else constantValue = 0
                        binaryNodeType = SqlExpressionType.GreaterThanOrEqual;
                    break;
                case ExpressionType.LessThan:
                    binaryNodeType = SqlExpressionType.LessThan;
                    // < 0 or < 1 both should be treated as less than
                    break;
                case ExpressionType.LessThanOrEqual:
                    // string.Compare(str1, str2) <= 1          (str1 < str1)
                    // string.Compare(str1, str2) <= 0          (str1 <= str1)
                    if (constantValue == 1)
                        binaryNodeType = SqlExpressionType.LessThan;
                    else // else constantValue = 0
                        binaryNodeType = SqlExpressionType.LessThanOrEqual;
                    break;
                case ExpressionType.Equal:
                    // string.Compare(str1, str2) == 1          (str1 > str1)
                    // string.Compare(str1, str2) == 0          (str1 == str2)
                    if (constantValue == 1)
                        binaryNodeType = SqlExpressionType.GreaterThan;
                    else // else constantValue = 0
                        binaryNodeType = SqlExpressionType.Equal;
                    break;
                case ExpressionType.NotEqual:
                    // string.Compare(str1, str2) != 1          (str1 <= str1)
                    // string.Compare(str1, str2) != 0          (str1 != str2)
                    if (constantValue == 1)
                        binaryNodeType = SqlExpressionType.LessThanOrEqual;
                    else // else constantValue = 0
                        binaryNodeType = SqlExpressionType.NotEqual;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported binary expression type: {this.Expression.NodeType}.");
            }

            return this.SqlFactory.CreateBinary(str1, str2, binaryNodeType);
        }

        private class StringCompareMethodConverter : LinqToSqlExpressionConverterBase<MethodCallExpression>
        {
            public StringCompareMethodConverter(IConversionContext context, MethodCallExpression expression, ExpressionConverterBase<Expression, SqlExpression>[] converters) : base(context, expression, converters)
            {
            }

            public override SqlExpression Convert(SqlExpression[] convertedChildren)
            {
                // stringField1.CompareTo(stringField2)
                // string.Compare(stringField1, stringField2)
                if (convertedChildren.Length < 2)
                    throw new InvalidOperationException("String.CompareTo / String.Compare requires at least 2 arguments.");

                return this.SqlFactory.CreateCollection(convertedChildren.Take(2));
            }
        }
    }
}
