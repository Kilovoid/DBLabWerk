using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    public interface ITableRow
    {
        void FillTable(IExcelDataReader reader);
    }
}
