using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Repositories
{
    public interface IExhibitRepository
    {
        void EnsureCreated();
        List<ExhibitTableRow> GetAll();
    }
}
