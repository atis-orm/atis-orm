using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Services
{
    public class SqlDataTypeFactory : ISqlDataTypeFactory
    {
        public ISqlDataType CreateDate()
        {
            return new SqlDataType(typeof(DateTime), SqlDataTypeNames.Date, length: null, isNullable: false, isUnicode: false, precision: null, scale: null);
        }
    }
}
