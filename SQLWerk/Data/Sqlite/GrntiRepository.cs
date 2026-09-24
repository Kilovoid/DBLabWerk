using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using SQLWerk.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Sqlite
{
    internal class GrntiRepository : IGrntiRepository
    {
        private const string InsertSql = """
            INSERT INTO Grnti
                (Codrub,Rubrika)
            VALUES
                ($c,$r);
            """;
        private readonly IConnectionFactory _factory;

        public GrntiRepository(IConnectionFactory factory) => _factory = factory;
        public void EnsureCreated()
        {
            using var conn = _factory.Create();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Grnti (
                    Id INTEGER PRIMARY KEY,
                    Codrub TEXT, Rubrika TEXT
                );
                """;
            cmd.ExecuteNonQuery();
        }

        public List<GrntiTableRow> GetAll()
        {
            var list = new List<GrntiTableRow>();
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id,Codrub,Rubrika
                FROM Grnti;
                """;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new GrntiTableRow
                {
                    Id = r.GetLong(0),
                    Codrub = r.GetStringOrNull(1),
                    Rubrika = r.GetStringOrNull(2),
                });
            }
            return list;
        }

        public int Count()
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Grnti;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void SaveAll(IEnumerable<GrntiTableRow> rows)
        {
            using var conn = _factory.Create();
            conn.Open();
            using var tx = conn.BeginTransaction();

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = InsertSql;

            foreach (var row in rows)
            {
                cmd.Parameters.Clear();
                cmd.AddParam("$c", row.Codrub);
                cmd.AddParam("$r", row.Rubrika);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }
}
