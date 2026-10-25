using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Extensions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Sqlite
{
    internal class ExhibitRepository : IExhibitRepository
    {
        private const string InsertSql = """
            INSERT INTO Exhibits
                (Codvuz,Z2,Type,Regnumber,Subject,Grnti,Bossname,Bosstitle,Exhitype,Vystavki,Exponat)
            VALUES
                ($c,$z,$t,$r,$s,$g,$bn,$bt,$e,$v,$ex);
            """;
        private readonly IConnectionFactory _factory;

        public ExhibitRepository(IConnectionFactory factory) => _factory = factory;
        public void EnsureCreated()
        {
            using var conn = _factory.Create();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Exhibits (
                    Id INTEGER PRIMARY KEY,
                    Codvuz INTEGER, Z2 TEXT, Type TEXT, Regnumber TEXT, Subject TEXT,
                    Grnti TEXT, Bossname TEXT, Bosstitle TEXT, Exhitype TEXT, Vystavki TEXT, Exponat TEXT
                );
                """;
            cmd.ExecuteNonQuery();
        }

        public List<ExhibitTableRow> GetAll()
        {
            var list = new List<ExhibitTableRow>();
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id,Codvuz,Z2,Type,Regnumber,Subject,Grnti,Bossname,Bosstitle,Exhitype,Vystavki,Exponat
                FROM Exhibits;
                """;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ExhibitTableRow
                {
                    Id = r.GetLong(0),
                    Codvuz = r.GetInt32(1),
                    Z2 = r.GetStringOrNull(2),
                    Type = r.GetStringOrNull(3),
                    Regnumber = r.GetStringOrNull(4),
                    Subject = r.GetStringOrNull(5),
                    Grnti = r.GetStringOrNull(6),
                    Bossname = r.GetStringOrNull(7),
                    Bosstitle = r.GetStringOrNull(8),
                    Exhitype = r.GetStringOrNull(9),
                    Vystavki = r.GetStringOrNull(10),
                    Exponat = r.GetStringOrNull(11),
                });
            }
            return list;
        }

        public int Count()
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Exhibits;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void SaveAll(IEnumerable<ExhibitTableRow> rows)
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
                cmd.AddParam("$c", row.Codvuz);
                cmd.AddParam("$z", row.Z2);
                cmd.AddParam("$t", row.Type);
                cmd.AddParam("$r", row.Regnumber);
                cmd.AddParam("$s", row.Subject);
                cmd.AddParam("$g", row.Grnti);
                cmd.AddParam("$bn", row.Bossname);
                cmd.AddParam("$bt", row.Bosstitle);
                cmd.AddParam("$e", row.Exhitype);
                cmd.AddParam("$v", row.Vystavki);
                cmd.AddParam("$ex", row.Exponat);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        public void Add(ExhibitTableRow row)
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = InsertSql;
            cmd.AddParam("$c", row.Codvuz).AddParam("$z", row.Z2)
               .AddParam("$t", row.Type).AddParam("$r", row.Regnumber)
               .AddParam("$s", row.Subject).AddParam("$g", row.Grnti)
               .AddParam("$bn", row.Bossname).AddParam("$bt", row.Bosstitle)
               .AddParam("$e", row.Exhitype).AddParam("$v", row.Vystavki)
               .AddParam("$ex", row.Exponat);
            cmd.ExecuteNonQuery();
        }

        public void Update(ExhibitTableRow row)
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
        UPDATE Exhibits SET
            Codvuz=$c, Z2=$z, Type=$t, Regnumber=$r, Subject=$s,
            Grnti=$g, Bossname=$bn, Bosstitle=$bt, Exhitype=$e,
            Vystavki=$v, Exponat=$ex
        WHERE Id=$id;
        """;
            cmd.AddParam("$c", row.Codvuz).AddParam("$z", row.Z2)
               .AddParam("$t", row.Type).AddParam("$r", row.Regnumber)
               .AddParam("$s", row.Subject).AddParam("$g", row.Grnti)
               .AddParam("$bn", row.Bossname).AddParam("$bt", row.Bosstitle)
               .AddParam("$e", row.Exhitype).AddParam("$v", row.Vystavki)
               .AddParam("$ex", row.Exponat).AddParam("$id", row.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Exhibits WHERE Id=$id;";
            cmd.AddParam("$id", id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteByCodvuz(int codvuz)
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Exhibits WHERE Codvuz=$c;";
            cmd.AddParam("$c", codvuz);
            cmd.ExecuteNonQuery();
        }

        public void Clear()
        {
            using var conn = _factory.Create();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Exhibits;";
            cmd.ExecuteNonQuery();
        }
    }
}
