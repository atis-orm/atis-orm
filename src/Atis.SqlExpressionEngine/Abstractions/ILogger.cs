using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Abstractions
{
    public interface ILogger
    {
        void Indent();
        void Log(string logText);
        void Unindent();
    }
}
