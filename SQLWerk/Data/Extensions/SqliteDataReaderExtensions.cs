using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Extensions
{
    public static class SqliteDataReaderExtensions
    {
        public static string? GetStringOrNull(this SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? null : r.GetString(i);
        public static long GetLongOrNull(this SqliteDataReader r, int i) =>
            r.GetInt64(i);
    }
}
