using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SQLWerk.Data.Abstractions
{
    public interface IFilePicker
    {
        Task<string[]?> PickFilesAsync(string title, int minCount);
    }
}
