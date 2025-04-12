using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public interface ISqlDataType
    {
        Type ClrType { get; }
        string DbType { get; }
        bool IsNullable { get; }
        int? Length { get; }
        int? Precision { get; }
        int? Scale { get; }
        bool IsUnicode { get; }
        bool UseMaxLength { get; }
    }

    public class SqlDataType : ISqlDataType
    {
        public SqlDataType(Type clrType, string dbType, int? length, bool isNullable, bool isUnicode, int? precision, int? scale, bool useMaxLength)
        {
            this.ClrType = clrType;
            this.DbType = dbType;
            this.Length = length;
            this.IsNullable = isNullable;
            this.IsUnicode = isUnicode;
            this.Precision = precision;
            this.Scale = scale;
            this.UseMaxLength = useMaxLength;
        }

        public Type ClrType { get; }

        public string DbType { get; }

        public bool IsNullable { get; }

        public int? Length { get; }

        public int? Precision { get; }

        public int? Scale { get; }

        public bool IsUnicode { get; }

        public bool UseMaxLength { get; }

        public override bool Equals(object obj)
        {
            if (obj is SqlDataType other)
            {
                return this.ClrType == other.ClrType &&
                       this.DbType == other.DbType &&
                       this.Length == other.Length &&
                       this.IsNullable == other.IsNullable &&
                       this.IsUnicode == other.IsUnicode &&
                       this.Precision == other.Precision &&
                       this.Scale == other.Scale && 
                       this.UseMaxLength == other.UseMaxLength;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ClrType, this.DbType, this.Length, this.IsNullable, this.IsUnicode, this.Precision, this.Scale, this.UseMaxLength);
        }
    }
}
