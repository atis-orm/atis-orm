using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Atis.Expressions
{
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y); // Use reference equality for comparison
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj); // Use the default runtime hash code
        }
    }
}
