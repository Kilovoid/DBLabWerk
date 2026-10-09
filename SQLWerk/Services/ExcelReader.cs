using ExcelDataReader;
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
        static ExcelReader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public List<T> Parse<T>(string filePath) where T : ITableRow, new()
        {
            var result = new List<T>();

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            reader.Read();

            while (reader.Read())
            {
                var row = new T();
                row.FillTable(reader);
                if (ValidationService.IsValid(row))
                {
                    result.Add(row);
                }
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