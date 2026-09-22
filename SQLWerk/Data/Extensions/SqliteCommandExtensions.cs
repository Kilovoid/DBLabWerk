using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Extensions
{
    public static class SqliteCommandExtensions // Класс для человеческого написания команд добавления и тд потому что у нас много полей
    {
        public static SqliteCommand AddParam (this SqliteCommand cmd, string name, object? value)
        {
            cmd.Parameters.Add(new SqliteParameter(name, value ?? DBNull.Value)); // Чтобы было не C# null, a DBNull
            return cmd;
        }
    }
}
