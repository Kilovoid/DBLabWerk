using Microsoft.Data.Sqlite;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Services
{
    public interface IExcelReader
    {
        List<T> Parse<T>(string filePath) where T : ITableRow, new();
    }
}
