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
            _cs = $"Data Source={Path.GetFullPath(dbPath)}";

        public SqliteConnection Create() => new SqliteConnection(_cs);
    }
}
