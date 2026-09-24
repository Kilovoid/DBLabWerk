using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    public interface IRepository<T>
    {
        void EnsureCreated();
        List<T> GetAll();
        int Count();
        void SaveAll(IEnumerable<T> rows);
    }


    public interface IExhibitRepository : IRepository<ExhibitTableRow>{}


    public interface IVuzRepository : IRepository<VuzTableRow>{}


    public interface IGrntiRepository : IRepository<GrntiTableRow>{}
}
