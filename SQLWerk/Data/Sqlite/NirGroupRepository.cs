using Microsoft.Data.Sqlite;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.Data.Sqlite;

internal sealed class NirGroupRepository : INirGroupRepository
{
    private readonly IConnectionFactory _factory;
    public NirGroupRepository(IConnectionFactory factory) => _factory = factory;

    public void EnsureCreated()
    {
        using var connection = _factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Однократный переход со старого числового Id на название группы.
        bool legacy = false;
        using (var info = connection.CreateCommand())
        {
            info.Transaction = transaction;
            info.CommandText = "PRAGMA table_info(NirGroups);";
            using var reader = info.ExecuteReader();
            while (reader.Read())
                if (reader.GetString(1) == "Id") legacy = true;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        if (legacy)
        {
            command.CommandText = """
                CREATE TABLE NirGroups_New (
                    Code TEXT NOT NULL PRIMARY KEY CHECK(length(trim(Code)) > 0)
                );
                INSERT INTO NirGroups_New(Code)
                    SELECT Name FROM NirGroups AS groups
                    WHERE NOT (groups.Id=1 AND groups.Name='Группа НИР'
                        AND NOT EXISTS (SELECT 1 FROM NirGroupMembers WHERE GroupId=groups.Id));
                CREATE TABLE NirGroupMembers_New (
                    GroupCode TEXT NOT NULL,
                    ExhibitId INTEGER NOT NULL,
                    PRIMARY KEY (GroupCode, ExhibitId),
                    FOREIGN KEY (GroupCode) REFERENCES NirGroups_New(Code) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (ExhibitId) REFERENCES Exhibits(Id) ON DELETE CASCADE
                );
                INSERT INTO NirGroupMembers_New(GroupCode, ExhibitId)
                    SELECT groups.Name, members.ExhibitId
                    FROM NirGroupMembers AS members
                    JOIN NirGroups AS groups ON groups.Id = members.GroupId;
                DROP TABLE NirGroupMembers;
                DROP TABLE NirGroups;
                ALTER TABLE NirGroups_New RENAME TO NirGroups;
                ALTER TABLE NirGroupMembers_New RENAME TO NirGroupMembers;
                """;
            command.ExecuteNonQuery();
        }

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS NirGroups (
                Code TEXT NOT NULL PRIMARY KEY CHECK(length(trim(Code)) > 0)
            );
            CREATE TABLE IF NOT EXISTS NirGroupMembers (
                GroupCode TEXT NOT NULL,
                ExhibitId INTEGER NOT NULL,
                PRIMARY KEY (GroupCode, ExhibitId),
                FOREIGN KEY (GroupCode) REFERENCES NirGroups(Code) ON DELETE CASCADE ON UPDATE CASCADE,
                FOREIGN KEY (ExhibitId) REFERENCES Exhibits(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_NirGroupMembers_ExhibitId ON NirGroupMembers(ExhibitId);
            """;
        command.ExecuteNonQuery();
        command.CommandText = "PRAGMA foreign_key_check(NirGroupMembers);";
        using (var reader = command.ExecuteReader())
            if (reader.Read())
                throw new InvalidOperationException("Нарушены связи групп НИР. Изменения отменены.");
        transaction.Commit();
    }

    public List<NirGroup> GetGroups()
    {
        using var connection = _factory.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Code FROM NirGroups ORDER BY Code;";
        using var reader = command.ExecuteReader();
        var groups = new List<NirGroup>();
        while (reader.Read())
            groups.Add(new NirGroup { Code = reader.GetString(0) });
        return groups;
    }

    public void CreateGroup(string code)
    {
        code = NormalizeCode(code);
        using var connection = _factory.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO NirGroups(Code) VALUES ($code);";
        command.Parameters.AddWithValue("$code", code);
        command.ExecuteNonQuery();
    }

    public bool DeleteGroup(string groupCode)
    {
        using var connection = _factory.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM NirGroups WHERE Code=$code;";
        command.Parameters.AddWithValue("$code", NormalizeCode(groupCode));
        return command.ExecuteNonQuery() > 0;
    }

    public List<long> GetExhibitIds(string groupCode)
    {
        using var connection = _factory.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ExhibitId FROM NirGroupMembers WHERE GroupCode=$group ORDER BY ExhibitId;";
        command.Parameters.AddWithValue("$group", NormalizeCode(groupCode));
        using var reader = command.ExecuteReader();
        var ids = new List<long>();
        while (reader.Read()) ids.Add(reader.GetInt64(0));
        return ids;
    }

    public int AddMembers(string groupCode, IEnumerable<long> exhibitIds) =>
        ChangeMembers(new[] { groupCode }, exhibitIds, true);

    public int AddMembers(IEnumerable<string> groupCodes, IEnumerable<long> exhibitIds) =>
        ChangeMembers(groupCodes, exhibitIds, true);

    public int RemoveMembers(string groupCode, IEnumerable<long> exhibitIds) =>
        ChangeMembers(new[] { groupCode }, exhibitIds, false);

    private int ChangeMembers(IEnumerable<string> groupCodes, IEnumerable<long> ids, bool add)
    {
        var codes = groupCodes.Select(NormalizeCode).Distinct(StringComparer.Ordinal).ToArray();
        var exhibitIds = ids.Distinct().ToArray();
        if (codes.Length == 0)
            throw new ArgumentException("Выберите хотя бы одну группу НИР.");

        using var connection = _factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        // Проверяем весь набор заранее. Автоматического создания групп нет.
        command.CommandText = "SELECT COUNT(*) FROM NirGroups WHERE Code=$group;";
        var groupParameter = command.Parameters.Add("$group", SqliteType.Text);
        foreach (var code in codes)
        {
            groupParameter.Value = code;
            if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                throw new InvalidOperationException($"Группа «{code}» не существует. Сначала создайте её.");
        }

        command.CommandText = add
            ? "INSERT INTO NirGroupMembers(GroupCode,ExhibitId) VALUES ($group,$exhibit) ON CONFLICT(GroupCode,ExhibitId) DO NOTHING;"
            : "DELETE FROM NirGroupMembers WHERE GroupCode=$group AND ExhibitId=$exhibit;";
        var exhibitParameter = command.Parameters.Add("$exhibit", SqliteType.Integer);
        int changed = 0;
        foreach (var code in codes)
        {
            groupParameter.Value = code;
            foreach (var id in exhibitIds)
            {
                exhibitParameter.Value = id;
                changed += command.ExecuteNonQuery();
            }
        }
        transaction.Commit();
        return changed;
    }

    private static string NormalizeCode(string code)
    {
        code = (code ?? "").Trim();
        if (code.Length == 0) throw new ArgumentException("Введите название группы.");
        return code;
    }
}
