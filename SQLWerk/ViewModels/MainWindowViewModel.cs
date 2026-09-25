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
        private readonly IExhibitRepository _exhibitRepo;
        private readonly IVuzRepository _vuzRepo;
        private readonly IGrntiRepository _grntiRepo;

        [ObservableProperty]
        private ObservableCollection<ExhibitTableRow> _exhibits = new();
        [ObservableProperty]
        private ObservableCollection<VuzTableRow> _vuzes = new();
        [ObservableProperty]
        private ObservableCollection<GrntiTableRow> _grnti = new();

        [ObservableProperty]
        private string _status = "Loading...";
        [ObservableProperty]
        private int _rowCount;

        [ObservableProperty]
        private bool _showExhibits = true;
        [ObservableProperty]
        private bool _showVuz;
        [ObservableProperty]
        private bool _showGrnti;

        public MainWindowViewModel(IExcelReader reader,
            IExhibitRepository exhibitRepo, IVuzRepository vuzRepo,
            IGrntiRepository grntiRepo)
        {
            _reader = reader;
            _exhibitRepo = exhibitRepo;
            _vuzRepo = vuzRepo;
            _grntiRepo = grntiRepo;
        }

        public void Initialize(string xlsPath)
        {
            _exhibitRepo.EnsureCreated();
            _vuzRepo.EnsureCreated();
            _grntiRepo.EnsureCreated();

            if (_exhibitRepo.Count() == 0 && File.Exists(xlsPath))
            {
                var read = _reader.Parse(xlsPath);
                _exhibitRepo.SaveAll(read);
                Status = $"Imported {read.Count} rows from {Path.GetFileName(xlsPath)}";
            }

            Exhibits = new ObservableCollection<ExhibitTableRow>(_exhibitRepo.GetAll());
            Vuzes = new ObservableCollection<VuzTableRow>(_vuzRepo.GetAll());
            Grnti = new ObservableCollection<GrntiTableRow>(_grntiRepo.GetAll());

            SelectExhibitsCommand.Execute(null);
        }

        [RelayCommand]
        public void SelectExhibits()
        {
            ShowExhibits = true;
            ShowVuz = false;
            ShowGrnti = false;

            RowCount = Exhibits.Count;
            Status = $"Exhibits: {RowCount} rows";
        }

        [RelayCommand]
        public void SelectVuz()
        {
            ShowExhibits = false;
            ShowVuz = true;
            ShowGrnti = false;

            RowCount = Exhibits.Count;
            Status = $"Vuz: {RowCount} rows";
        }

        [RelayCommand]
        public void SelectGrnti()
        {
            ShowExhibits = false;
            ShowVuz = false;
            ShowGrnti = true;

            RowCount = Exhibits.Count;
            Status = $"Grnti: {RowCount} rows";
        }
    }


        public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
        {
            public string Status => IsValid ? "OK" : "Error";

            public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
    }
}
