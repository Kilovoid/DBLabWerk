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
        private string? _exhibitsPath;
        private string? _vuzPath;
        private string? _grntiPath;


        private readonly ImportService<ExhibitTableRow> _exhibitImport;
        private readonly ImportService<VuzTableRow> _vuzImport;
        private readonly ImportService<GrntiTableRow> _grntiImport;
        private readonly IFullDataRepository _fullDataRepo;
        private readonly IDataBaseMaintenanceService _maintenance;

        [ObservableProperty]
        private ObservableCollection<ExhibitTableRow> _exhibits = new();

        [ObservableProperty]
        private ObservableCollection<VuzTableRow> _vuzes = new();

        [ObservableProperty]
        private ObservableCollection<GrntiTableRow> _grnti = new();

        [ObservableProperty]
        private ObservableCollection<FullModel> _fullTable = new();

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

        [ObservableProperty]
        private bool _showAll;

        [ObservableProperty]
        private bool _isBusy;

        public MainWindowViewModel(
            ImportService<ExhibitTableRow> exhibitImport,
            ImportService<VuzTableRow> vuzImport,
            ImportService<GrntiTableRow> grntiImport,
            IFullDataRepository fullDataRepo,
            IDataBaseMaintenanceService maintenance)
        {
            _exhibitImport = exhibitImport;
            _vuzImport = vuzImport;
            _grntiImport = grntiImport;
            _fullDataRepo = fullDataRepo;
            _maintenance = maintenance;                
        }

        public void Initialize(string exhibitsPath, string vuzPath, string grntiPath)
        {
            _exhibitsPath = exhibitsPath;
            _vuzPath = vuzPath;
            _grntiPath = grntiPath;

            LoadAll();
        }

        private void LoadAll()
        {
            if (_exhibitsPath is null || _vuzPath is null || _grntiPath is null)
                return;

            _exhibitImport.ImportIfEmpty(_exhibitsPath);
            _vuzImport.ImportIfEmpty(_vuzPath);
            _grntiImport.ImportIfEmpty(_grntiPath);

            _fullDataRepo.EnsureCreated();
            FullTable = new ObservableCollection<FullModel>(_fullDataRepo.GetAll());

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
            ShowAll = false;

            RowCount = Exhibits.Count;
            Status = $"Exhibits: {RowCount} rows";
        }

        [RelayCommand]
        public void SelectVuz()
        {
            ShowExhibits = false;
            ShowVuz = true;
            ShowGrnti = false;
            ShowAll = false;

            RowCount = Vuzes.Count;
            Status = $"Vuz: {RowCount} rows";
        }

        [RelayCommand]
        public void SelectGrnti()
        {
            ShowExhibits = false;
            ShowVuz = false;
            ShowAll = false;
            ShowGrnti = true;

            RowCount = Grnti.Count;
            Status = $"Grnti: {RowCount} rows";
        }

        [RelayCommand]
        public void SelectFull()
        {
            ShowAll = true;
            ShowExhibits = false;
            ShowGrnti = false;
            ShowVuz = false;

            RowCount = FullTable.Count;
            Status = $"Full data: {RowCount} rows";
        }

        [RelayCommand]
        private void Reload()
        {
            if (_exhibitsPath is null || _vuzPath is null || _grntiPath is null)
            {
                Status = "Paths are not initialized";
                return;
            }

            try
            {
                IsBusy = true;
                Status = "Reloading DB... ";

                _maintenance.ReloadDataBase();

                _exhibitImport.ImportIfEmpty(_exhibitsPath);
                _vuzImport.ImportIfEmpty(_vuzPath);
                _grntiImport.ImportIfEmpty(_grntiPath);

                _fullDataRepo.EnsureCreated();
                FullTable = new ObservableCollection<FullModel>(_fullDataRepo.GetAll());

                Exhibits = new ObservableCollection<ExhibitTableRow>(_exhibitImport.LoadAll());
                Vuzes = new ObservableCollection<VuzTableRow>(_vuzImport.LoadAll());
                Grnti = new ObservableCollection<GrntiTableRow>(_grntiImport.LoadAll());

                if (ShowAll) SelectFullCommand.Execute(null);
                else if (ShowVuz) SelectVuzCommand.Execute(null);
                else if (ShowGrnti) SelectGrntiCommand.Execute(null);
                else SelectExhibitsCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Status = $"Err loading: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }


        public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
        {
            public string Status => IsValid ? "OK" : "Error";

            public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
    }
}
