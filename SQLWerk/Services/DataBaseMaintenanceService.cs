using Microsoft.Data.Sqlite;
using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SQLWerk.Services
{
    internal class DataBaseMaintenanceService : IDataBaseMaintenanceService
    {
        private readonly IConnectionFactory _factory;
        private readonly IFullDataRepository _fullDataRepo;
        private readonly string _dbPath;

        public DataBaseMaintenanceService(IConnectionFactory factory,
            IFullDataRepository fullDataRepo,
            string dbPath)
        {
            _factory = factory;
            _fullDataRepo = fullDataRepo;
            _dbPath = dbPath;
        }

        public void ReloadDataBase()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
            RecreateSchema();
        }

        public void RecreateSchema()
        {
            using var conn = _factory.Create();
            conn.Open();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = """
                DROP VIEW  IF EXISTS AllDataView;
                DROP TABLE IF EXISTS Exhibits;
                DROP TABLE IF EXISTS Vuz;
                DROP TABLE IF EXISTS Grnti;

                CREATE TABLE Exhibits (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codvuz    TEXT, Z2 TEXT, Type TEXT, Regnumber TEXT,
                    Subject   TEXT, Grnti TEXT, Bossname TEXT, Bosstitle TEXT,
                    Exhitype  TEXT, Vystavki TEXT, Exponat TEXT
                );

                CREATE TABLE Vuz (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codvuz    TEXT, Z2 TEXT, Z1 TEXT, Z1Full TEXT,
                    Region    TEXT, City TEXT, Status TEXT,
                    Obl       TEXT, OblName TEXT, GrVed TEXT, Prof TEXT
                );

                CREATE TABLE Grnti (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codrub    TEXT, Rubrika TEXT
                );
            """;
            cmd.ExecuteNonQuery();

            _fullDataRepo.EnsureCreated();
        }
    }
}
