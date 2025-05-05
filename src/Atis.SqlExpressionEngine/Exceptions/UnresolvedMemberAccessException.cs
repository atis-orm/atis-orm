using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Exceptions
{
    public class UnresolvedMemberAccessException : Exception
    {
        public UnresolvedMemberAccessException(ModelPath modelPath) : base($"Unable to resolve ModelPath '{modelPath}'. This error can occur because not all the members of the class were selected in `Select` and then a non selected member was used in later calls, e.g. .Select(x => new MyModel {{ P1 = x.Property1 }}).OrderBy(x => x.Property2), here `Property2` was not selected in `Select` call.")
        {
        }
    }
}
