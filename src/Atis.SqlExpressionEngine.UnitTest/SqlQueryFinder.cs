using Atis.SqlExpressionEngine.SqlExpressions;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public class SqlQueryFinder : SqlExpressionVisitor
    {
        private int indentCount = 0;
        private readonly HashSet<Guid> ids = new HashSet<Guid>();

        public override SqlExpression? Visit(SqlExpression node)
        {
            if(node is null)
                return null;

            indentCount++;
            try
            {
                this.Log($"Visiting {node.GetType().Name}");
                if (node is SqlQueryExpression q)
                {
                    if (ids.Contains(q.Id))
                        throw new InvalidOperationException("Cycle detected");
                    else
                        ids.Add(q.Id);
                }
                return base.Visit(node);
            }
            finally
            {
                indentCount--;
            }
        }

        private void Log(string message)
        {
            Console.WriteLine($"{new string(' ', indentCount * 2)}{message}");
        }
    }
}
