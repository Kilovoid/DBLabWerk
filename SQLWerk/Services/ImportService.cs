using Microsoft.Extensions.Logging;
using SQLWerk.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SQLWerk.Services
{
    public class ImportService<T> where T : ITableRow, new()
    {
        private readonly IExcelReader _reader;
        private readonly IRepository<T> _repo;
        private readonly ILogger<ImportService<T>> _logger;

        public ImportService(IExcelReader reader, IRepository<T> repo,
            ILogger<ImportService<T>> logger)
        {
            _reader = reader;
            _repo = repo;
            _logger = logger;
        }

        public (int imported, int total) ImportIfEmpty(string? xlsPath)
        {
            _repo.EnsureCreated();

            if (_repo.Count() > 0) return (0, _repo.Count());
            if (xlsPath is null || !File.Exists(xlsPath)) return (0, 0);

            var read = Dedup(_reader.Parse<T>(xlsPath));
            _repo.SaveAll(read);
            return (read.Count, read.Count);
        }

        public (int imported, int total) ForceImport(string? xlsPath)
        {
            _repo.EnsureCreated();
            _repo.Clear();
            if (xlsPath is null || !File.Exists(xlsPath)) return (0, 0);

            var read = Dedup(_reader.Parse<T>(xlsPath));
            _repo.SaveAll(read);
            return (read.Count, read.Count);
        }

        public int Count() => _repo.Count();

        private List<T> Dedup(List<T> rows)
        {
            var result = new List<T>(rows.Count);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fileName = "";

            int removed = 0;
            foreach (var r in rows)
            {
                var key = r.DedupKey;
                if (string.IsNullOrEmpty(key))
                {
                    result.Add(r);
                    continue;
                }

                if (seen.Add(key))
                {
                    result.Add(r);
                }
                else
                {
                    removed++;
                    _logger.LogWarning(
                        "Дубликат в файле {File}: {Type} с ключом '{Key}' пропущен",
                        fileName, typeof(T).Name, key);
                }
            }

            return result;
        }
    }
}