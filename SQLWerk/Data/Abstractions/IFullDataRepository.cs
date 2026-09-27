using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    public interface IFullDataRepository
    {
        void EnsureCreated();
        List<FullModel> GetAll();
    }
}
