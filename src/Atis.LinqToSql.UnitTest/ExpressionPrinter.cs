using Atis.LinqToSql.ExpressionExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.LinqToSql.UnitTest
{
    public class ExpressionPrinter : ExpressionVisitor
    {
        private readonly StringBuilder sb = new StringBuilder();
        private int currentIndent;
        private const string tab = "    ";

        public string Print(Expression expression)
        {
            sb.Clear();
            currentIndent = 0;
            this.Visit(expression);
            return sb.ToString();
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count > 1)
                this.Append("(");
            for(var i = 0; i < node.Parameters.Count; i++)
            {
                this.Append(node.Parameters[i].Name ?? "{no name}");
                if(i < node.Parameters.Count - 1)
                    this.Append(", ");
            }
            if (node.Parameters.Count > 1)
                this.Append(")");
            this.Append(" => ");
            this.Visit(node.Body);
            return node;
        }

        public static string PrintExpression(Expression expression)
        {
            var printer = new ExpressionPrinter();
            return printer.Print(expression);
        }


        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            if (node is null) return node;

            var previousLength = sb.Length;

            var updatedNode = base.Visit(node);
            if (sb.Length == previousLength)
            {
                sb.Append(node.ToString());
            }
            return updatedNode;
        }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is SubQueryNavigationExpression subQueryNav)
            {
                this.Append("SubQueryNavigation(");
                this.AppendLine();
                this.Indent();
                this.Append(subQueryNav.NavigationProperty);
                this.Append(", ");
                this.Visit(subQueryNav.Query);
                this.Unindent();
                this.AppendLine();
                this.Append(")");
                return node;
            }
            else if (node is NavigationExpression nav)
            {
                this.Append("Navigation(");
                this.AppendLine();
                this.Indent();
                this.Visit(nav.SourceExpression);
                this.Append(", ");
                this.AppendLine();
                this.Append($"\"{nav.NavigationProperty}\"");
                this.Append(", ");
                this.AppendLine();
                this.Visit(nav.JoinedDataSource);
                this.Append(", ");
                this.AppendLine();
                this.Visit(nav.JoinCondition);
                this.AppendLine();
                this.Unindent();
                this.Append(")");

                return node;
            }
            else if (node is ChildJoinExpression childJoin)
            {
                this.Append("ChildJoin(");
                this.AppendLine();
                this.Indent();
                this.Append($"\"{childJoin.NavigationName}\"");
                this.Append(", ");
                this.AppendLine();
                this.Visit(childJoin.Query);
                this.Append(", ");
                this.AppendLine();
                this.Visit(childJoin.Parent);
                this.Append(", ");
                this.AppendLine();
                this.Visit(childJoin.JoinCondition);
                this.AppendLine();
                this.Unindent();
                this.Append(")");
                return node;
            }

            return base.VisitExtension(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            this.Append("new {");
            this.AppendLine();
            this.Indent();
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var arg = node.Arguments[i];
                this.Append(node.Members?.Skip(i)?.FirstOrDefault()?.Name ?? "{no name}");
                this.Append(" = ");
                this.Visit(arg);
                if (i < node.Arguments.Count - 1)
                    this.Append(",");
                this.AppendLine();
            }
            this.Unindent();
            this.Append("}");
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            base.Visit(node.Left);
            this.Append($" {GetBinaryOperator(node.NodeType)} ");
            base.Visit(node.Right);
            return node;
        }

        private static string GetBinaryOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "==";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                default:
                    return nodeType.ToString();
            }
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            var updatedNode = base.VisitInvocation(node);
            this.Append("()");
            return updatedNode;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var updatedNode = base.VisitConstant(node);
            if (node.Value is IQueryable)
            {
                var type = node.Type.GetGenericArguments().First();
                this.Append($"DataSet<{type.Name}>");
            }
            else
            {
                if (node.Value is string || node.Value is DateTime || node.Value is Guid)
                    this.Append("\"");
                this.Append(node.Value?.ToString() ?? "{null}");
                if (node.Value is string || node.Value is DateTime || node.Value is Guid)
                    this.Append("\"");
            }
            return updatedNode;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (this.IsConstant(node))
            {
                this.Append(node.Member.Name);
                return node;
            }
            else
            {
                var updatedNode = base.VisitMember(node);
                this.Append(".");
                this.Append(node.Member.Name);
                return updatedNode;
            }
        }

        private bool IsConstant(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
            {
                return this.IsConstant(memberExpression.Expression);
            }
            else if (expression is ConstantExpression)
            {
                return true;
            }
            return false;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var updatedNode = base.VisitParameter(node);
            this.Append(node.Name ?? "{no name}");
            return updatedNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(QueryExtensions.DataSet) &&
                node.Method.DeclaringType == typeof(QueryExtensions))
            {
                this.Append($"DataSet<{node.Method.GetGenericArguments()[0].Name}>");
                return node;
            }

            if (node.Object != null)
            {
                this.Visit(node.Object);
                this.Append(".");
            }
            this.Append(node.Method.Name);
            this.Append("(");
            var isQueryMethod = node.Method.DeclaringType == typeof(System.Linq.Queryable) ||
                                    node.Method.DeclaringType == typeof(QueryExtensions);
            if (isQueryMethod)
            {
                this.AppendLine();
                this.Indent();
            }
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var arg = node.Arguments[i];
                this.Visit(arg);
                if (i < node.Arguments.Count - 1)
                {
                    this.Append(", ");
                }
                if (isQueryMethod)
                    this.AppendLine();
            }
            if (isQueryMethod)
                this.Unindent();
            this.Append(")");
            return node;
        }

        private void Append(string text)
        {
            sb.Append(text);
        }

        private void AppendLine()
        {
            sb.AppendLine();
            if (currentIndent > 0)
                sb.Append(string.Concat(Enumerable.Repeat(tab, currentIndent)));
        }

        private void Indent()
        {
            currentIndent++;
            sb.Append(tab);
        }

        private void Unindent()
        {
            if (currentIndent > 0)
                currentIndent--;
            sb.Remove(sb.Length - tab.Length, tab.Length);
        }
    }
}
