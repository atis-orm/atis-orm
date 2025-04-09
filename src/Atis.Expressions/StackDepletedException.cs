using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.Expressions
{
    public class StackDepletedException : Exception
    {
        public StackDepletedException() : base("No more elements in the stack.") { }
    }
}
