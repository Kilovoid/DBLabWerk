using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Services;
using Avalonia.Platform.Storage;
using System.IO;
using ExcelDataReader;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;

namespace SQLWerk.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IExcelReader _reader;
        private readonly IRepository _repo;

        [ObservableProperty]
        private ObservableCollection<ExhibitTableRow> rows = new();
        [ObservableProperty]
        private string _status = "Loading...";
        [ObservableProperty]
        private int _rowCount;

        public MainWindowViewModel()
        {
            _reader = null!;
            _repo = null!;
        }

        public MainWindowViewModel(IExcelReader reader, IRepository repo)
        {
            _reader = reader;
            _repo = repo;
        }

        public void Initialize(string xlsPath)
        {
            _repo.EnsureCreated();

            if (_repo.Count() == 0 && File.Exists(xlsPath))
            {
                var read = _reader.Parse(xlsPath);
                _repo.SaveAll(read);
                Status = $"Imported {read.Count} rows from {Path.GetFileName(xlsPath)}";
            }
            else
            {
                Status = "Loaded from DB";
            }

            var all = _repo.GetAll();
            Rows = new ObservableCollection<ExhibitTableRow>(all);
            RowCount = all.Count;
            Status += $"DB contains total {RowCount} rows";
        }
    }
        

    public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
    {
        public string Status => IsValid ? "OK" : "Error";

        public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
    }
}
