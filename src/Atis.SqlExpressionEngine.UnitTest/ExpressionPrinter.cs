using Atis.SqlExpressionEngine.ExpressionExtensions;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Atis.SqlExpressionEngine.UnitTest
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
                this.Append($"\"{subQueryNav.NavigationProperty}\"");
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
            else if (node is InValuesExpression inValues)
            {
                this.Append("In(");
                this.Visit(inValues.Expression);
                this.Append(", ");
                this.Visit(inValues.Values);
                this.Append(")");
                return node;
            }

            return base.VisitExtension(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            this.Append("{ ");
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                this.Visit(node.Expressions[i]);
                if (i < node.Expressions.Count - 1)
                    this.Append(", ");
            }
            this.Append(" }");
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count > 1)
                this.Append("(");
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                this.Append(node.Parameters[i].Name ?? "{no name}");
                if (i < node.Parameters.Count - 1)
                    this.Append(", ");
            }
            if (node.Parameters.Count > 1)
                this.Append(")");
            this.Append(" => ");
            this.Visit(node.Body);
            return node;
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

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            this.Append("new ");
            this.Append(node.NewExpression.Type.Name);
            this.Append(" {");
            this.AppendLine();
            this.Indent();
            for (var i = 0; i < node.Bindings.Count; i++)
            {
                var binding = node.Bindings[i];
                if (binding is MemberAssignment memberAssignment)
                {
                    this.Append(binding.Member.Name);
                    this.Append(" = ");
                    this.Visit(memberAssignment.Expression);
                }
                else
                {
                    this.Append(binding.Member.Name);
                    this.Append(" = n/a");
                }
                if (i < node.Bindings.Count - 1)
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

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                    this.Append("((");
                    this.Append(node.Type.Name);
                    this.Append(") ");
                    this.Visit(node.Operand);
                    this.Append(")");
                    break;
                case ExpressionType.Not:
                    this.Append("!(");
                    this.Visit(node.Operand);
                    this.Append(")");
                    break;
                default:
                    this.Append(node.NodeType.ToString());
                    this.Append("(");
                    this.Visit(node.Operand);
                    this.Append(")");
                    break;
            }
            return node;
        }

        private static string GetBinaryOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => nodeType.ToString()
            };
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var updatedNode = base.VisitConstant(node);
            if (node.Value is IQueryable)
            {
                var type = node.Type.GetGenericArguments().FirstOrDefault();
                this.Append($"DataSet<{type?.Name ?? "Unknown"}>");
            }
            else
            {
                if (node.Value is string or DateTime or Guid)
                    this.Append("\"");
                this.Append(node.Value?.ToString() ?? "{null}");
                if (node.Value is string or DateTime or Guid)
                    this.Append("\"");
            }
            return updatedNode;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (this.IsConstant(node))
            {
                var members = new List<string>();
                members.Add(node.Member.Name);
                while (node.Expression is MemberExpression memberExpression)
                {
                    members.Add(memberExpression.Member.Name);
                    node = memberExpression;
                }
                members.Reverse();
                this.Append(string.Join(".", members));
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

        private bool IsConstant(Expression? expression)
        {
            return expression switch
            {
                MemberExpression memberExpression => this.IsConstant(memberExpression.Expression),
                ConstantExpression => true,
                _ => false
            };
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
        
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            this.Visit(node.Expression);
            this.Append("(");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                this.Visit(node.Arguments[i]);
                if (i < node.Arguments.Count - 1)
                    this.Append(", ");
            }
            this.Append(")");
            return node;
        }
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            this.Visit(node.Test);
            this.Append(" ? ");
            this.Visit(node.IfTrue);
            this.Append(" : ");
            this.Visit(node.IfFalse);
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
            sb.Length = Math.Max(0, sb.Length - tab.Length);
        }
    }
}
