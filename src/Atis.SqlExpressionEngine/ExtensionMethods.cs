using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Atis.SqlExpressionEngine
{
    public static class ExtensionMethods
    {
        public static bool AllEqual<T>(this IEnumerable<T> seq1, IEnumerable<T> seq2)
        {
            if (ReferenceEquals(seq1, seq2)) return true;
            if (seq1 == null) return seq2 == null || !seq2.Any();
            if (seq2 == null) return !seq1.Any();
            return seq1.SequenceEqual(seq2);
        }

        public static ModelPath GetModelPath(this MemberExpression memberExpression)
        {
            var pathElements = new List<string>();
            do
            {
                pathElements.Add(memberExpression.Member.Name);
                memberExpression = memberExpression.Expression as MemberExpression;
            } while (memberExpression != null);
            return new ModelPath(pathElements.Reverse<string>().ToArray());
        }

        public static bool TryGetArgLambda(this MethodCallExpression methodCall, int argIndex, out LambdaExpression lambdaExpression)
        {
            if (methodCall is null)
                throw new ArgumentNullException(nameof(methodCall));

            var arg = methodCall.Arguments.Skip(argIndex).FirstOrDefault();
            lambdaExpression = (arg as UnaryExpression)?.Operand as LambdaExpression
                                        ??
                                    arg as LambdaExpression;
            return lambdaExpression != null;
        }

        public static bool TryGetArgLambdaParameter(this MethodCallExpression methodCall, int argIndex, int paramIndex, out ParameterExpression parameterExpression)
        {
            if (TryGetArgLambda(methodCall, argIndex, out var lambdaExpression))
            {
                parameterExpression = lambdaExpression.Parameters.Skip(paramIndex).FirstOrDefault();
                return parameterExpression != null;
            }
            parameterExpression = null;
            return false;
        }

        public static ParameterExpression GetArgLambdaParameterRequired(this MethodCallExpression methodCall, int argIndex, int paramIndex)
        {
            if (TryGetArgLambdaParameter(methodCall, argIndex, paramIndex, out var parameterExpression))
            {
                return parameterExpression;
            }
            throw new ArgumentException($"Unable to extract ParameterExpression from method '{methodCall.Method.Name}' at argument index {argIndex} and parameter index {paramIndex}, make sure argument is LambdaExpression.");
        }

        public static bool SqlExpressionsAreEqual(SqlExpression sqlExpression1, SqlExpression sqlExpression2)
        {
            if (sqlExpression1 is null && sqlExpression2 is null)
            {
                return true;
            }
            if (sqlExpression1 is null || sqlExpression2 is null)
            {
                return false;
            }
            var hash1 = SqlExpressionHashGenerator.GenerateHash(sqlExpression1);
            var hash2 = SqlExpressionHashGenerator.GenerateHash(sqlExpression2);
            return hash1 == hash2;
        }

        public static SqlExpressionBinding PrependPath(this SqlExpressionBinding actualBinding, ModelPath pathToPrepend)
        {
            if (actualBinding is null)
                throw new ArgumentNullException(nameof(actualBinding));
            if (actualBinding is NonProjectableBinding)
                return new NonProjectableBinding(actualBinding.SqlExpression, pathToPrepend.Append(actualBinding.ModelPath));
            else
                return new SqlExpressionBinding(actualBinding.SqlExpression, pathToPrepend.Append(actualBinding.ModelPath));
        }

        public static SqlExpressionBinding UpdateExpression(this SqlExpressionBinding actualBinding, SqlExpression sqlExpression)
        {
            if (actualBinding is null)
                throw new ArgumentNullException(nameof(actualBinding));
            if (actualBinding is NonProjectableBinding)
                return new NonProjectableBinding(sqlExpression, actualBinding.ModelPath);
            else
                return new SqlExpressionBinding(sqlExpression, actualBinding.ModelPath);
        }
    }
}
