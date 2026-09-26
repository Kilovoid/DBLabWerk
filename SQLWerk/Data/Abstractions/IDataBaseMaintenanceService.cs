using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    public interface IDataBaseMaintenanceService
    {
        void ReloadDataBase();
        void RecreateSchema();
    }
}
