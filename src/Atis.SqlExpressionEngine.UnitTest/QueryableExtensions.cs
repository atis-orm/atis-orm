using Atis.SqlExpressionEngine.UnitTest.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public static class QueryableExtensions
    {
        [SqlFunction("string_agg")]
        public static string String_Agg<GroupingType, EntityType>(this IGrouping<GroupingType, EntityType> groupingQuery, Expression<Func<EntityType, string?>> stringFieldSelector, string separator)
        {
            throw new NotImplementedException();
        }
    }
}
