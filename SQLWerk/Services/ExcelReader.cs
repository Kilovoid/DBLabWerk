using ExcelDataReader;
using Microsoft.Extensions.Logging;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SQLWerk.Services
{
    public class ExcelReader : IExcelReader
    {
        private readonly ILogger<ExcelReader> _logger;
        static ExcelReader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public ExcelReader(ILogger<ExcelReader> logger) => _logger = logger;

        public List<T> Parse<T>(string filePath) where T : ITableRow, new()
        {
            var result = new List<T>();
            var fileName = Path.GetFileName(filePath);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            reader.Read();

            int rowNumber = 1;
            while (reader.Read())
            {
                rowNumber++;
                var row = new T();
                row.FillTable(reader);

                var (ok, reason) = ValidationService.Validate(row);
                if (ok) result.Add(row);
                else
                    _logger.LogWarning(
                        "Строка {Row} в файле {File} удалена. Причина: {Reason}",
                        rowNumber, fileName, reason ?? "—");
            }

            return result;
        }

        internal static string? Get(IExcelDataReader reader, int idx)
        {
            if (idx >= reader.FieldCount) return null;

            var v = reader.GetValue(idx);
            return v?.ToString();
        }

        public string[] GetHeaders(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            if (!reader.Read()) return Array.Empty<string>();

            var headers = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                headers[i] = reader.GetValue(i)?.ToString()?.Trim() ?? "";
            }
            return headers;
        }
    }
}