using ExcelDataReader;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
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
                result.Add(row);
            }
            return result;
        }

        internal static string? Get(IExcelDataReader reader, int idx)
        {
            if (idx >= reader.FieldCount) return null;

            var v = reader.GetValue(idx);
            return v?.ToString();
        }
    }
}