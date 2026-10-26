using Microsoft.Data.Sqlite;
using SQLWerk.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLWerk.Data.Sqlite
{
    internal class SqliteConnectionFactory : IConnectionFactory
    {
        private readonly string _cs;

        public SqliteConnectionFactory(string dbPath) =>
            _cs = new SqliteConnectionStringBuilder
            {
                DataSource = Path.GetFullPath(dbPath),
                ForeignKeys = true
            }.ToString();

        public SqliteConnection Create() => new SqliteConnection(_cs);
    }
}
