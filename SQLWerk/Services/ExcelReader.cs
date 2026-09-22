using ExcelDataReader;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLWerk.Services
{
    public class ExcelReader
    {
        static ExcelReader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public List<ExhibitTableRow> Read (string filePath)
        {
            var result = new List<ExhibitTableRow>();

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            reader.Read();

            while(reader.Read())
            {
                result.Add(
                    new ExhibitTableRow
                    {
                        Codvuz = Get(reader, 0),
                        Z2 = Get(reader, 1),
                        Type = Get(reader, 2),
                        Regnumber = Get(reader, 3),
                        Subject = Get(reader, 4),
                        Grnti = Get(reader, 5),
                        Bossname = Get(reader, 6),
                        Bosstitle = Get(reader, 7),
                        Exhitype = Get(reader, 8),
                        Vystavki = Get(reader, 9),
                        Exponat = Get(reader, 10),
                    });
            }
            return result;
        }

        private static string? Get(IExcelDataReader reader, int idx)
        {
            if (idx >= reader.FieldCount) return null;

            var v = reader.GetValue(idx);
            return v?.ToString();
        }
    }
}
