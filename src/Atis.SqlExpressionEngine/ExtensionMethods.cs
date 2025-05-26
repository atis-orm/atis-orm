using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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

        public static string GetPath(this MemberExpression memberExpression)
        {
            if (memberExpression is null)
                throw new ArgumentNullException(nameof(memberExpression));
            var path = memberExpression.Member.Name;
            var current = memberExpression.Expression;
            while (current is MemberExpression member)
            {
                path = $"{member.Member.Name}.{path}";
                current = member.Expression;
            }
            return path;
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

        public static LambdaExpression GetArgLambdaRequired(this MethodCallExpression methodCall, int argIndex)
        {
            if (TryGetArgLambda(methodCall, argIndex, out var lambdaExpression))
            {
                return lambdaExpression;
            }
            throw new ArgumentException($"Unable to extract LambdaExpression from method '{methodCall.Method.Name}' at argument index {argIndex}, make sure argument is LambdaExpression.");
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

        public static LambdaExpression ExtractLambda(this Expression expression)
        {
            return (expression as UnaryExpression)?.Operand as LambdaExpression
                    ??
                    expression as LambdaExpression;
        }

        public static LambdaExpression ExtractLambdaRequired(this Expression expression)
            => ExtractLambda(expression)
                ?? throw new ArgumentException($"Unable to extract LambdaExpression from expression '{expression}'.");

        public static IReadOnlyList<SqlExpression> FlattenQueryShape(this SqlQueryShapeExpression queryShape)
        {
            var list = new List<SqlExpression>();
            addExpressions(queryShape, list);
            return list;

            void addExpressions(SqlExpression sqlExpression, List<SqlExpression> sqlExpressionList)
            {
                if (sqlExpression is SqlMemberInitExpression qs)
                {
                    foreach (var binding in qs.Bindings)
                    {
                        addExpressions(binding.SqlExpression, sqlExpressionList);
                    }
                }
                else
                    sqlExpressionList.Add(sqlExpression);
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            if (list is null)
                return true;
            if (list is ICollection<T> collection)
                return collection.Count == 0;
            using (var enumerator = list.GetEnumerator())
            {
                return !enumerator.MoveNext();
            }
        }

        public static IReadOnlyList<SelectColumn> ConvertQueryShapeToSelectList(SqlExpression queryShape, bool applyAll)
        {
            var selectList = new List<SelectColumn>();
            FillSelectList(columnAlias: null, queryShape, selectList, applyAll: applyAll);
            return selectList;
        }

        public static void FillSelectList(string columnAlias, SqlExpression sqlExpression, List<SelectColumn> selectList, bool applyAll)
        {
            if (sqlExpression is SqlMemberInitExpression queryShape)
            {
                foreach (var binding in queryShape.Bindings)
                {
                    if (binding.Projectable || applyAll)
                        FillSelectList(binding.MemberName, binding.SqlExpression, selectList, applyAll);
                }
            }
            else if (sqlExpression is SqlDataSourceQueryShapeExpression dsQueryShape)
            {
                FillSelectList(columnAlias, dsQueryShape.ShapeExpression, selectList, applyAll);
            }
            else
            {
                var columnAliasToSet = columnAlias ?? "Col1";

                if (selectList.Any(x => x.Alias == columnAliasToSet))
                    columnAliasToSet = GenerateUniqueColumnAlias(new HashSet<string>(selectList.Select(x => x.Alias)), columnAliasToSet);

                selectList.Add(new SelectColumn(sqlExpression, columnAliasToSet ?? "Col1", scalarColumn: columnAlias == null));
            }
        }

        private static string GenerateUniqueColumnAlias(HashSet<string> aliases, string columnAlias)
        {
            int i = 1;
            var newColumnAlias = $"{columnAlias}_{i}";
            while (aliases.Contains(newColumnAlias))
            {
                i++;
                newColumnAlias = $"{columnAlias}_{i}";
            }
            return newColumnAlias;
        }

        public static T CastTo<T>(this SqlExpression sqlExpression, string exceptionMessage = null) where T : SqlExpression
        {
            if (sqlExpression is null)
                throw new ArgumentNullException(nameof(sqlExpression));
            if (!(exceptionMessage is null))
                exceptionMessage = $" {exceptionMessage}";
            return sqlExpression as T
                ??
                throw new InvalidCastException($"Unable to cast SqlExpression '{sqlExpression.GetType().Name}' to '{typeof(T).Name}'.{exceptionMessage}");
        }
    }
}
