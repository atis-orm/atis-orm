using Atis.LinqToSql.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.LinqToSql.Abstractions
{
    public interface ISqlExpressionPostprocessor
    {
        void Initialize();
        SqlExpression Postprocess(SqlExpression sqlExpression);
    }
}
