using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Extensions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Sqlite
{
    internal class FullDataRepository : IFullDataRepository
    {
        private readonly IConnectionFactory _factory;

        public FullDataRepository(IConnectionFactory factory) => _factory = factory;

        public void EnsureCreated()
        {
            using var conn = _factory.Create();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
            CREATE VIEW IF NOT EXISTS AllDataView AS
            SELECT
                e.Id           AS ExhibitId,
                e.Codvuz, e.Z2, e.Type, e.Regnumber, e.Subject, e.Grnti,
                e.Bossname, e.Bosstitle, e.Exhitype, e.Vystavki, e.Exponat,

                v.Id           AS VuzId,
                v.Z1, v.Z1Full, v.Region, v.City, v.Status,
                v.Obl, v.OblName, v.GrVed, v.Prof,

                g.Id           AS GrntiId,
                g.Codrub, g.Rubrika
            FROM Exhibits e
            LEFT JOIN Vuz   v ON v.Id = e.Id
            LEFT JOIN Grnti g ON g.Id = e.Id;
            """;
            cmd.ExecuteNonQuery();
        }

        public List<FullModel> GetAll()
        {
            var list = new List<FullModel>();

            using var conn = _factory.Create();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
    SELECT
        ExhibitId, Codvuz, Z2, Type, Regnumber, Subject, Grnti,
        Bossname, Bosstitle, Exhitype, Vystavki, Exponat,
        VuzId, Z1, Z1Full, Region, City, Status, Obl, OblName, GrVed, Prof,
        GrntiId, Codrub, Rubrika
    FROM AllDataView;
    """;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new FullModel
                {
                    Id = r.GetInt64(0),     
                    Codvuz = r.GetStringOrNull(1),
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
                    Z1 = r.GetStringOrNull(13),
                    Z1Full = r.GetStringOrNull(14),
                    Region = r.GetStringOrNull(15),
                    City = r.GetStringOrNull(16),
                    Status = r.GetStringOrNull(17),
                    Obl = r.GetStringOrNull(18),
                    OblName = r.GetStringOrNull(19),
                    GrVed = r.GetStringOrNull(20),
                    Prof = r.GetStringOrNull(21),
                    Codrub = r.GetStringOrNull(23),
                    Rubrika = r.GetStringOrNull(24),
                });
            }

            return list;
        }
    }
}