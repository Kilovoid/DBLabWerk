using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Data.Abstractions
{
    internal interface IConnectionFactory
    {
        SqliteConnection Create();
    }
}
