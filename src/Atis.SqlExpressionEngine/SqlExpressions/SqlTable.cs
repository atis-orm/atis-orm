using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlTable
    {
        public SqlTable(string tableName)
            : this(tableName, schema: null, database: null, server: null)
        {
        }

        public SqlTable(string tableName, string schema, string database, string server)
        {
            this.TableName = tableName ?? throw new ArgumentNullException(nameof(tableName), "Table name cannot be null.");
            this.Schema = schema;
            this.Database = database;
            this.Server = server;
        }

        public string TableName { get; }
        public string Schema { get; }
        public string Database { get; }
        public string Server { get; }
    }
}
