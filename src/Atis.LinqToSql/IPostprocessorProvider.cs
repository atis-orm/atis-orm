using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql
{
    public interface IPostprocessorProvider
    {
        SqlExpression Process(SqlExpression sqlExpression);
    }
}
