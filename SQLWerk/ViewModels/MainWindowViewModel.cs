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
        private readonly ImportService<ExhibitTableRow> _exhibitImport;
        private readonly ImportService<VuzTableRow> _vuzImport;
        private readonly ImportService<GrntiTableRow> _grntiImport;

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

        public MainWindowViewModel(ImportService<ExhibitTableRow> exhibitImport,
            ImportService<VuzTableRow> vuzImport,
            ImportService<GrntiTableRow> grntiImport)
        {
            _exhibitImport = exhibitImport;
            _vuzImport = vuzImport;
            _grntiImport = grntiImport;
        }

        public void Initialize(string exhibitsPath, string vuzPath, string grntiPath)
        {
            _exhibitImport.ImportIfEmpty(exhibitsPath);
            _vuzImport.ImportIfEmpty(vuzPath);
            _grntiImport.ImportIfEmpty(grntiPath);

            Exhibits = new ObservableCollection<ExhibitTableRow>(_exhibitImport.LoadAll());
            Vuzes = new ObservableCollection<VuzTableRow>(_vuzImport.LoadAll());
            Grnti = new ObservableCollection<GrntiTableRow>(_grntiImport.LoadAll());

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

            RowCount = Vuzes.Count;
            Status = $"Vuz: {RowCount} rows";
        }

        [RelayCommand]
        public void SelectGrnti()
        {
            ShowExhibits = false;
            ShowVuz = false;
            ShowGrnti = true;

            RowCount = Grnti.Count;
            Status = $"Grnti: {RowCount} rows";
        }
    }


        public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
        {
            public string Status => IsValid ? "OK" : "Error";

            public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
    }
}
