using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class FilterCondition
    {
        public FilterCondition(SqlExpression predicate, bool useOrOperator)
        {
            this.Predicate = predicate;
            this.UseOrOperator = useOrOperator;
        }

        public SqlExpression Predicate { get; set; }
        public bool UseOrOperator { get; set; }
    }
}