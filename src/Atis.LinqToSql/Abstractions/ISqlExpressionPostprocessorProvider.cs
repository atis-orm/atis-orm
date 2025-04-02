using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql.Abstractions
{
    public interface ISqlExpressionPostprocessorProvider
    {
        SqlExpression Postprocess(SqlExpression sqlExpression);
    }
}
