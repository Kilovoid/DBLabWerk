using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace SQLWerk.Services;

public sealed class ReportPreferences
{
    private readonly string _connectionString;
    public ReportPreferences()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SQLWerk");
        Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder
        { DataSource = Path.Combine(directory, "report-preferences.db") }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS ReportPreferences (
                Id INTEGER PRIMARY KEY CHECK(Id=1), LastNumber INTEGER NOT NULL, Directory TEXT NOT NULL);
            INSERT OR IGNORE INTO ReportPreferences VALUES (1, 0, '');
            """;
        command.ExecuteNonQuery();
    }

    public (long NextNumber, string Directory) Read()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT LastNumber, Directory FROM ReportPreferences WHERE Id=1;";
        using var reader = command.ExecuteReader();
        reader.Read();
        return (checked(reader.GetInt64(0) + 1), reader.GetString(1));
    }

    public void Remember(long number, string directory)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE ReportPreferences SET LastNumber=MAX(LastNumber,$number), Directory=$directory WHERE Id=1;";
        command.Parameters.AddWithValue("$number", number);
        command.Parameters.AddWithValue("$directory", directory);
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
