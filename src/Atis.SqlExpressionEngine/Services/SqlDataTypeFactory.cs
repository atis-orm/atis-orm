using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;


namespace Atis.SqlExpressionEngine.Services
{
    public class SqlDataTypeFactory : ISqlDataTypeFactory
    {
        public ISqlDataType CreateDate()
        {
            return new SqlDataType(typeof(DateTime), SqlDataTypeNames.Date, length: null, isNullable: false, isUnicode: false, precision: null, scale: null, useMaxLength: false);
        }

        public ISqlDataType CreateNonUnicodeString(int length)
        {
            return new SqlDataType(typeof(string), SqlDataTypeNames.NonUnicodeString, length: length > 0 ? (int?)length : null, isNullable: false, isUnicode: false, precision: null, scale: null, useMaxLength: length <= 0);
        }
    }
}
