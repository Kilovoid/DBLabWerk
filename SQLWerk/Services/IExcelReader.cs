using Microsoft.Data.Sqlite;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Services
{
    public interface IExcelReader
    {
        List<ExhibitTableRow> Parse(string filePath);
    }
}
