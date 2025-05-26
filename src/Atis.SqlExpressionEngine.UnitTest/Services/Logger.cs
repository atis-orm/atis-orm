using Atis.SqlExpressionEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Services
{
    internal class Logger : ILogger
    {
        private int indentation;

        public void Indent()
        {
            this.indentation++;
        }

        public void Log(string logText)
        {
            var indent = new string('\t', this.indentation);
            Console.WriteLine($"{indent}{logText}");
        }

        public void Unindent()
        {
            this.indentation--;
        }
    }
}
