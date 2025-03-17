using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql
{
    public interface IPostprocessor
    {
        void Initialize();
        SqlExpression Process(SqlExpression sqlExpression);
    }
}
