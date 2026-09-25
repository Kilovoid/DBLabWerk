using ExcelDataReader;
using SQLWerk.Data.Abstractions;
using SQLWerk.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Models
{
    public class ExhibitTableRow : ITableRow
    {
        public long Id { get; set; }
        public string? Codvuz { get; set; }
        public string? Z2 { get; set; }
        public string? Type { get; set; }
        public string? Regnumber { get; set; }
        public string? Subject { get; set; }
        public string? Grnti { get; set; }
        public string? Bossname { get; set; }
        public string? Bosstitle { get; set; }
        public string? Exhitype { get; set; }
        public string? Vystavki { get; set; }
        public string? Exponat { get; set; }

        public void FillTable(IExcelDataReader r)
        {
            Codvuz = ExcelReader.Get(r, 0);
            Z2 = ExcelReader.Get(r, 1);
            Type = ExcelReader.Get(r, 2);
            Regnumber = ExcelReader.Get(r, 3);
            Subject = ExcelReader.Get(r, 4);
            Grnti = GrntiService.ParseGrnti(ExcelReader.Get(r, 5));
            Bossname = ExcelReader.Get(r, 6);
            Bosstitle = ExcelReader.Get(r, 7);
            Exhitype = ExcelReader.Get(r, 8);
            Vystavki = ExcelReader.Get(r, 9);
            Exponat = ExcelReader.Get(r, 10);
        }
    }
}
