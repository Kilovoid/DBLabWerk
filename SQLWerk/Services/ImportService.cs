using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Services
{
    internal class ImportService
    {
        private readonly IExcelReader _reader;
        private readonly IExhibitRepository _repo;

        public ImportService(IExcelReader reader, IExhibitRepository repo)
        {
            _reader = reader;
            _repo = repo;
        }

        public (int imported, int total) ImportIfEmpty(string xlsPath)
        {
            _repo.EnsureCreated();

            if (_repo.Count() > 0)
            {
                return (0, _repo.Count());
            }

            var read = _reader.Parse(xlsPath);
            _repo.SaveAll(read);
            return (read.Count, read.Count);
        }

        public List<ExhibitTableRow> LoadAll() => _repo.GetAll();
    }
}
