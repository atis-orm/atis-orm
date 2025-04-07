using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Abstractions
{
    public interface ISqlExpressionPostprocessorProvider
    {
        SqlExpression Postprocess(SqlExpression sqlExpression);
    }
}
