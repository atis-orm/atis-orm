using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Exceptions
{
    public class UnresolvedMemberAccessException : Exception
    {
        public UnresolvedMemberAccessException(string expression, SqlExpressionType parentNodeType) : base($"MemberExpressionConverter is unable to find any matching `SqlExpression` in Data Source for `MemberExpression` '{expression}'. The parent expression extracted was '{parentNodeType}'. This error can occur because of these reasons (a) `ModelPath` is not correctly mapped in Data Source or Projection. For example, q.Select(x => new {{ f1 = new {{ p1 = x.Field1 }} }}).Select(x => x.f1.p1), in this example, when creating `SqlColumnExpression`, `ModelPath` for 1st `SqlColumnExpression` should be 'f1.p1', but if it is `Empty` or only 'p1' then it will not match with any projection in 2nd Select and will cause this error, x.f1.p1 (ModePath = f1.p1), while projection model path is 'p1', (b) not all the members were selected in `Select` and then a non selected member was used in later calls, e.g. .Select(x => new MyModel {{ P1 = x.Property1 }}).OrderBy(x => x.Property2), here `Property2` was not selected in `Select` call.")
        {
        }
    }
}
