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
        void Clear();
    }


    public interface IExhibitRepository : IRepository<ExhibitTableRow>
    {
        void Add(ExhibitTableRow row);
        void Update(ExhibitTableRow row);
        void Delete(long id);
        void DeleteByCodvuz(string codvuz);
    }


    public interface IVuzRepository : IRepository<VuzTableRow>
    {
        void Add(VuzTableRow row);
        void Update(VuzTableRow row);
        void Delete(long id);
    }


    public interface IGrntiRepository : IRepository<GrntiTableRow>
    {
        void Add(GrntiTableRow row);
        void Update(GrntiTableRow row);
        void Delete(long id);
    }
}
