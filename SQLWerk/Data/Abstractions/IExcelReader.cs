using Microsoft.Data.Sqlite;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    public interface IExcelReader
    {
        List<T> Parse<T>(string filePath) where T : ITableRow, new();
        string[] GetHeaders(string filePath);
    }
}
