using Microsoft.Data.Sqlite;
using SQLWerk.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.Services;

// Один выбранный набор удаляется целиком, либо транзакция откатывается.
internal sealed class BatchDeleteService : IBatchDeleteService
{
    private readonly IConnectionFactory _factory;

    public BatchDeleteService(IConnectionFactory factory) => _factory = factory;

    public (int Deleted, int Related) DeleteExhibits(IEnumerable<long> ids) =>
        InTransaction((connection, transaction) =>
            (DeleteIds(connection, transaction, "Exhibits", ids), 0));

    public (int Deleted, int Related) DeleteVuzes(IEnumerable<long> ids)
    {
        var selected = ids.Distinct().ToArray();
        return InTransaction((connection, transaction) =>
        {
            var codes = ReadCodes(connection, transaction, "Vuz", "Codvuz", selected);
            int related = 0;
            foreach (var code in codes)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM Exhibits WHERE Codvuz=$code;";
                command.Parameters.AddWithValue("$code", int.Parse(code));
                related += command.ExecuteNonQuery();
            }
            return (DeleteIds(connection, transaction, "Vuz", selected), related);
        });
    }

    public (int Deleted, int Related) DeleteGrnti(IEnumerable<long> ids)
    {
        var selected = ids.Distinct().ToArray();
        return InTransaction((connection, transaction) =>
        {
            var rubrics = ReadCodes(connection, transaction, "Grnti", "Codrub", selected);
            var exhibits = new List<long>();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT Id, Grnti FROM Exhibits;";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var raw = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var codes = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(code => code.Trim())
                        .Where(code => code.Length >= 2)
                        .Select(code => code.Substring(0, 2));
                    if (codes.Any(rubrics.Contains))
                        exhibits.Add(reader.GetInt64(0));
                }
            }

            int related = DeleteIds(connection, transaction, "Exhibits", exhibits);
            return (DeleteIds(connection, transaction, "Grnti", selected), related);
        });
    }

    private (int Deleted, int Related) InTransaction(
        Func<SqliteConnection, SqliteTransaction, (int Deleted, int Related)> action)
    {
        using var connection = _factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var result = action(connection, transaction);
        transaction.Commit();
        return result;
    }

    // Имена таблиц и столбцов передаются только из методов этого класса.
    private static HashSet<string> ReadCodes(SqliteConnection connection, SqliteTransaction transaction,
        string table, string column, IEnumerable<long> ids)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table} WHERE Id=$id;";
        var parameter = command.Parameters.Add("$id", SqliteType.Integer);
        foreach (var id in ids)
        {
            parameter.Value = id;
            var value = command.ExecuteScalar();
            if (value != null && value != DBNull.Value)
                result.Add(Convert.ToString(value)!.Trim());
        }
        return result;
    }

    private static int DeleteIds(SqliteConnection connection, SqliteTransaction transaction,
        string table, IEnumerable<long> ids)
    {
        int deleted = 0;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {table} WHERE Id=$id;";
        var parameter = command.Parameters.Add("$id", SqliteType.Integer);
        foreach (var id in ids.Distinct())
        {
            parameter.Value = id;
            deleted += command.ExecuteNonQuery();
        }
        return deleted;
    }
}
