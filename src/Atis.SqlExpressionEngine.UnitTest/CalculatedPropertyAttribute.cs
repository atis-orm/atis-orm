using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public class CalculatedPropertyAttribute : Attribute
    {
        public string ExpressionPropertyName { get; set; }
        public CalculatedPropertyAttribute(string expressionPropertyName)
        {
            this.ExpressionPropertyName = expressionPropertyName;
        }
    }
}
