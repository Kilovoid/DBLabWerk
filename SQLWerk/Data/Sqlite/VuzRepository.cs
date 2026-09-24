using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Extensions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Sqlite
{
    internal class VuzRepository : IVuzRepository
    {
        private const string InsertSql = """
            INSERT INTO Vuz
                (Codvuz,Z1,Z1Full,Z2,Region,City,Status,Obl,OblName,GrVed,Prof)
            VALUES
                ($co,$z1,$z1f,$z2,$r,$ci,$st,$ob,$on,$gv,$p);
            """;
        private readonly IConnectionFactory _factory;

        public VuzRepository(IConnectionFactory factory) => _factory = factory;
        public void EnsureCreated()
        {
            using var conn = _factory.Create();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Vuz (
                    Id INTEGER PRIMARY KEY,
                    Codvuz TEXT, Z1 TEXT, Z1Full TEXT, Z2 TEXT, Region TEXT,
                    City TEXT, Status TEXT, Obl TEXT, OblName TEXT, GrVed TEXT, Prof TEXT
                );
                """;
            cmd.ExecuteNonQuery();
        }

        public List<VuzTableRow> GetAll()
        {
            var list = new List<VuzTableRow>();
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id,Codvuz,Z1,Z1Full,Z2,Region,City,Status,Obl,OblName,GrVed,Prof
                FROM Vuz;
                """;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new VuzTableRow
                {
                    Id = r.GetLong(0),
                    Codvuz = r.GetStringOrNull(1),
                    Z1 = r.GetStringOrNull(2),
                    Z1Full = r.GetStringOrNull(3),
                    Z2 = r.GetStringOrNull(4),
                    Region = r.GetStringOrNull(5),
                    City = r.GetStringOrNull(6),
                    Status = r.GetStringOrNull(7),
                    Obl = r.GetStringOrNull(8),
                    OblName = r.GetStringOrNull(9),
                    GrVed = r.GetStringOrNull(10),
                    Prof = r.GetStringOrNull(11),
                });
            }
            return list;
        }

        public int Count()
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Vuz;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void SaveAll(IEnumerable<VuzTableRow> rows)
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
                cmd.AddParam("$co", row.Codvuz);
                cmd.AddParam("$z1", row.Z1);
                cmd.AddParam("$z1f", row.Z1Full);
                cmd.AddParam("$z2", row.Z2);
                cmd.AddParam("$r", row.Region);
                cmd.AddParam("$ci", row.City);
                cmd.AddParam("$st", row.Status);
                cmd.AddParam("$ob", row.Obl);
                cmd.AddParam("$on", row.OblName);
                cmd.AddParam("$gv", row.GrVed);
                cmd.AddParam("$p", row.Prof);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }
}
