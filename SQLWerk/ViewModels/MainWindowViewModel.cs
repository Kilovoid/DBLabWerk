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
        private readonly IExhibitRepository _exhibitRepo;
        private readonly IVuzRepository _vuzRepo;
        private readonly IGrntiRepository _grntiRepo;
        private readonly IFullDataRepository _fullDataRepo;
        private readonly IFilePicker _picker;
        private readonly IExcelReader _reader;

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
            IExhibitRepository exhibitRepo,
            IVuzRepository vuzRepo,
            IGrntiRepository grntiRepo,
            IFullDataRepository fullDataRepo,
            IFilePicker picker,
            IExcelReader reader)
        {
            _exhibitImport = exhibitImport;
            _vuzImport = vuzImport;
            _grntiImport = grntiImport;
            _fullDataRepo = fullDataRepo;
            _picker = picker;
            _reader = reader;
            _exhibitRepo = exhibitRepo;
            _vuzRepo = vuzRepo;
            _grntiRepo = grntiRepo;
        }

        public void Initialize()
        {
            _exhibitRepo.EnsureCreated();
            _vuzRepo.EnsureCreated();
            _grntiRepo.EnsureCreated();
            _fullDataRepo.EnsureCreated();

            var haveData =
                _exhibitImport.Count() > 0 || _vuzImport.Count() > 0 || _grntiImport.Count() > 0;
            if (haveData)
            {
                ReloadFromDatabase();
                Status = "Данные загружены из БД";
            }
            else
            {
                Status = "Выберите файлы для импорта таблиц";
            } 
        }

        private void ReloadFromDatabase()
        {
            FullTable = new ObservableCollection<FullModel>(_fullDataRepo.GetAll());
            Exhibits = new ObservableCollection<ExhibitTableRow>(_exhibitRepo.GetAll());
            Vuzes = new ObservableCollection<VuzTableRow>(_vuzRepo.GetAll());
            Grnti = new ObservableCollection<GrntiTableRow>(_grntiRepo.GetAll());

            SelectExhibitsCommand.Execute(null);
        }
        private void LoadAll() //depricated for now
        {
            if (_exhibitsPath is null || _vuzPath is null || _grntiPath is null)
                return;

            _exhibitImport.ImportIfEmpty(_exhibitsPath);
            _vuzImport.ImportIfEmpty(_vuzPath);
            _grntiImport.ImportIfEmpty(_grntiPath);

            FullTable = new ObservableCollection<FullModel>(_fullDataRepo.GetAll());

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
            ShowAll = false;

            RowCount = Exhibits.Count;
            Status = $"Выставки: {RowCount} строк";
        }

        [RelayCommand]
        public void SelectVuz()
        {
            ShowExhibits = false;
            ShowVuz = true;
            ShowGrnti = false;
            ShowAll = false;

            RowCount = Vuzes.Count;
            Status = $"ВУЗы: {RowCount} строк";
        }

        [RelayCommand]
        public void SelectGrnti()
        {
            ShowExhibits = false;
            ShowVuz = false;
            ShowAll = false;
            ShowGrnti = true;

            RowCount = Grnti.Count;
            Status = $"ГРНТИ: {RowCount} строк";
        }

        [RelayCommand]
        public void SelectFull()
        {
            ShowAll = true;
            ShowExhibits = false;
            ShowGrnti = false;
            ShowVuz = false;

            RowCount = FullTable.Count;
            Status = $"Общие данные: {RowCount} строк";
        }

        [RelayCommand]
        
        private async Task LoadAsync()
        {
            var files = await _picker.PickFilesAsync("Выберите файлы, содержащие данные Vyst_mo, VUZ, grntirub", 3);
            if (files is null)
            {
                Status = "Загрузка отменена";
                return;
            }

            string? exhibitsPath = null;
            string? vuzPath = null;
            string? grntiPath = null;

            var errs = new List<string>();
            foreach (var file in files)
            {
                string err;
                if (exhibitsPath is null && HeaderValidator.IsMatch(file,
                    ExhibitTableRow.ExpectedHeader,
                    _reader,
                    out err))
                {
                    exhibitsPath = file;
                    continue;
                }

                if (vuzPath is null && HeaderValidator.IsMatch(file,
                    VuzTableRow.ExpectedHeader,
                    _reader,
                    out err))
                {
                    vuzPath = file;
                    continue;
                }

                if (grntiPath is null && HeaderValidator.IsMatch(file,
                    GrntiTableRow.ExpectedHeader,
                    _reader,
                    out err))
                {
                    grntiPath = file;
                    continue;
                }

                errs.Add($"Файл {Path.GetFileName(file)} не подходит");
            }

            if (exhibitsPath is null)
            {
                errs.Add("Файл с данными Vyst_mo не был найден!");
            }
            if (vuzPath is null)
            {
                errs.Add("Файл с данными VUZ не был найден!");
            }
            if (grntiPath is null)
            {
                errs.Add("Файл с данными grntirub не был найден!");
            }

            bool hasNull = (exhibitsPath is null || vuzPath is null || grntiPath is null);

            if (errs.Count > 0)
            {
                Status = "Ошибки при загрузке: \n" + string.Join("\n", errs);
            }

            if (errs.Count > 0 && hasNull)
            {
                Status = "Критические ошибки при загрузке: \n" + string.Join("\n", errs);
                return;
            }

            try
            {
                _exhibitImport.ForceImport(exhibitsPath!);
                _vuzImport.ForceImport(vuzPath!);
                _grntiImport.ForceImport(grntiPath!);

                ReloadFromDatabase();

                SelectExhibitsCommand.Execute(null);
                Status = $"Загружено: Выставки={Exhibits.Count}, ВУЗы={Vuzes.Count}, ГРНТИ={Grnti.Count}";
            }
            catch (Exception ex)
            {
                Status = "Ошибка импорта: " + ex.Message;
            }
        }
    }


        public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
        {
            public string Status => IsValid ? "OK" : "Error";

            public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
    }
}
