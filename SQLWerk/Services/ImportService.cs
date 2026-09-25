using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Sqlite;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Services
{
    public class ImportService<T> where T : ITableRow, new()
    {
        private readonly IExcelReader _reader;
        private readonly IRepository<T> _repo;
        public ImportService(IExcelReader reader, IRepository<T> repo)
        {
            _reader = reader;
            _repo = repo;
        }

        public (int imported, int total) ImportIfEmpty(string xlsPath)
        {
            _repo.EnsureCreated();
            if (_repo.Count() > 0) return (0, _repo.Count());

            var read = _reader.Parse<T>(xlsPath);
            _repo.SaveAll(read);
            return (read.Count, read.Count);
        }
        public List<T> LoadAll() => _repo.GetAll();
    }
}
