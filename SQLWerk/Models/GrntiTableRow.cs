using ExcelDataReader;
using SQLWerk.Data.Abstractions;
using SQLWerk.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SQLWerk.Models
{
    public class GrntiTableRow : ITableRow
    {
        public long Id { get; set; }
        public string? Codrub { get; set; }
        public string? Rubrika { get; set; }

        public void FillTable(IExcelDataReader r)
        {
            Codrub = ExcelReader.Get(r, 0);
            Rubrika = ExcelReader.Get(r, 1);
        }
    }
}
