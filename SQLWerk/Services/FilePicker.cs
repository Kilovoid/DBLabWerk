using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SQLWerk.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLWerk.Services
{
    public class FilePicker : IFilePicker
    {
        private readonly Window _owner;

        public FilePicker(Window owner)
        {
            _owner = owner;
        }
        public async Task<string[]?> PickFilesAsync(string title, int minCount)
        {
            var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Excel")
                    {
                        Patterns = new[] { "*.xls", "*.xlsx" }
                    }
                }
            });
            if (files.Count < minCount) return null;
            return files.Select(f => f.Path.LocalPath).ToArray();
        }
    }
}
