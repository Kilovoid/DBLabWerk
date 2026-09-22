using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    public interface IExhibitRepository
    {
        void EnsureCreated();
        List<ExhibitTableRow> GetAll();
        int Count();
        void SaveAll(IEnumerable<ExhibitTableRow> rows);
    }
}
