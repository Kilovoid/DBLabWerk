using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SQLWerk.Services;

// Однократный переход старой БД с TEXT на INTEGER. Новые таблицы уже имеют INTEGER.
internal static class CodvuzMigration
{
    public static void Apply(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        foreach (var table in new[] { "Vuz", "Exhibits" })
        {
            var columns = new List<string>();
            string codeType = "";
            using (var info = connection.CreateCommand())
            {
                info.Transaction = transaction;
                info.CommandText = $"PRAGMA table_info(\"{table}\");";
                using var reader = info.ExecuteReader();
                while (reader.Read())
                {
                    var column = reader.GetString(1);
                    columns.Add(column);
                    if (column.Equals("Codvuz", StringComparison.OrdinalIgnoreCase))
                        codeType = reader.GetString(2);
                }
            }

            if (codeType.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!codeType.Equals("TEXT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Неожиданный тип {table}.Codvuz: {codeType}");

            // Проверяем до CAST: SQLite иначе превращает некорректный текст в 0.
            var codes = new HashSet<int>();
            using (var check = connection.CreateCommand())
            {
                check.Transaction = transaction;
                check.CommandText = $"SELECT Id, Codvuz FROM \"{table}\";";
                using var reader = check.ExecuteReader();
                while (reader.Read())
                {
                    var text = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                    if (!int.TryParse(text, NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture, out var code) || code <= 0)
                        throw new InvalidOperationException(
                            $"{table}, Id={reader.GetInt64(0)}: код '{text}' нельзя преобразовать в положительный int.");
                    if (table == "Vuz" && !codes.Add(code))
                        throw new InvalidOperationException($"Повторяющийся код ВУЗа после преобразования: {code}");
                }
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name=$name;";
            command.Parameters.AddWithValue("$name", table);
            var create = (string)command.ExecuteScalar()!;
            command.Parameters.Clear();
            var definition = Regex.Replace(create[create.IndexOf('(')..],
                @"\bCodvuz\s+TEXT\b", "Codvuz INTEGER", RegexOptions.IgnoreCase);

            var names = string.Join(",", columns.ConvertAll(column => $"\"{column}\""));
            var values = string.Join(",", columns.ConvertAll(column =>
                column.Equals("Codvuz", StringComparison.OrdinalIgnoreCase)
                    ? "CAST(TRIM(Codvuz) AS INTEGER)" : $"\"{column}\""));
            command.CommandText = $"""
                DROP VIEW IF EXISTS AllDataView;
                CREATE TABLE "__{table}_Integer" {definition};
                INSERT INTO "__{table}_Integer" ({names}) SELECT {values} FROM "{table}";
                DROP TABLE "{table}";
                ALTER TABLE "__{table}_Integer" RENAME TO "{table}";
                """;
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }
}
