using ExcelDataReader;
using SQLWerk.Data.Abstractions;
using SQLWerk.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SQLWerk.Models
{
    public class VuzTableRow : ITableRow
    {
        public long Id { get; set; }
        public string? Codvuz { get; set; }
        public string? Z1 { get; set; }
        public string? Z1Full { get; set; }
        public string? Z2 { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? Status { get; set; }
        public string? Obl { get; set; }
        public string? OblName { get; set; }
        public string? GrVed { get; set; }
        public string? Prof { get; set; }

        public void FillTable(IExcelDataReader r)
        {
            Codvuz = ExcelReader.Get(r, 0);
            Z1 = ExcelReader.Get(r, 1);
            Z1Full = ExcelReader.Get(r, 2);
            Z2 = ExcelReader.Get(r, 3);
            Region = ExcelReader.Get(r, 4);
            City = ExcelReader.Get(r, 5);
            Status = ExcelReader.Get(r, 6);
            Obl = ExcelReader.Get(r, 7);
            OblName = ExcelReader.Get(r, 8);
            GrVed = ExcelReader.Get(r, 9);
            Prof = ExcelReader.Get(r, 10);
        }
    }
}
