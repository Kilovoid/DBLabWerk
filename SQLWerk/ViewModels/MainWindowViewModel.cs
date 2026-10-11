using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDataReader;
using Microsoft.Extensions.Logging;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using SQLWerk.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly InMemoryLoggerProvider _logProvider;
        private readonly CrossValidationService _crossValidator;

        [ObservableProperty] private bool _hasErrors;
        [ObservableProperty] private int _warningCount;

        [ObservableProperty] private ObservableCollection<ExhibitTableRow> _exhibits = new();
        [ObservableProperty] private ObservableCollection<VuzTableRow> _vuzes = new();
        [ObservableProperty] private ObservableCollection<GrntiTableRow> _grnti = new();
        [ObservableProperty] private ObservableCollection<FullModel> _fullTable = new();

        [ObservableProperty] private string _status = "Загрузка...";
        [ObservableProperty] private int _rowCount;

        [ObservableProperty] private bool _showExhibits = true;
        [ObservableProperty] private bool _showVuz;
        [ObservableProperty] private bool _showGrnti;
        [ObservableProperty] private bool _showAll;
        [ObservableProperty] private bool _showErrorScreen;
        [ObservableProperty] private string _errorScreenText = "";

        public event EventHandler? ShowLogRequested;
        public Func<GrntiTableRow?, Task<GrntiTableRow?>>? GrntiEditHandler { get; set; }
        public Func<string, string, Task<bool>>? ConfirmHandler { get; set; }
        public Func<VuzTableRow?, Task<VuzTableRow?>>? VuzEditHandler { get; set; }
        public Func<ExhibitTableRow?, Task<ExhibitTableRow?>>? ExhibitEditHandler { get; set; }
        public MainWindowViewModel(
            ImportService<ExhibitTableRow> exhibitImport,
            ImportService<VuzTableRow> vuzImport,
            ImportService<GrntiTableRow> grntiImport,
            IExhibitRepository exhibitRepo,
            IVuzRepository vuzRepo,
            IGrntiRepository grntiRepo,
            IFullDataRepository fullDataRepo,
            IFilePicker picker,
            IExcelReader reader,
            InMemoryLoggerProvider loggerProvider,
            CrossValidationService crossValidator)
        {
            _exhibitImport = exhibitImport;
            _vuzImport = vuzImport;
            _grntiImport = grntiImport;
            _exhibitRepo = exhibitRepo;
            _vuzRepo = vuzRepo;
            _grntiRepo = grntiRepo;
            _fullDataRepo = fullDataRepo;
            _picker = picker;
            _reader = reader;
            _logProvider = loggerProvider;
            _crossValidator = crossValidator;

            _logProvider.Changed += OnLogChanged;

            OnLogChanged(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ShowLog() => ShowLogRequested?.Invoke(this, EventArgs.Empty);

        private void OnLogChanged(object? sender, EventArgs e)
        {
            HasErrors = _logProvider.HasWarnings;
            WarningCount = _logProvider.Entries.Count(x => x.Level >= LogLevel.Warning);
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
                SelectExhibitsCommand.Execute(null);
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

            //SelectExhibitsCommand.Execute(null);
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
            CloseErrorScreenIfOpen();

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
            CloseErrorScreenIfOpen();

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
            CloseErrorScreenIfOpen();

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
            CloseErrorScreenIfOpen();

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
            _logProvider.Clear();

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

                var removed = _crossValidator.RemoveInvalidExhibits(
                    _exhibitRepo, _vuzRepo, _grntiRepo);

                ReloadFromDatabase();

                if (_logProvider.HasWarnings)
                {
                    ErrorScreenText = BuildErrorScreenText();
                    ShowErrorScreen = true;
                }
                else
                {
                    SelectExhibitsCommand.Execute(null);
                }

                Status = $"Загружено: Выставки={Exhibits.Count}, ВУЗы={Vuzes.Count}, ГРНТИ={Grnti.Count}";
            }
            catch (Exception ex)
            {
                Status = "Ошибка импорта: " + ex.Message;
            }
        }

        [RelayCommand]
        private async Task AddGrntiAsync()
        {
            if (GrntiEditHandler is null) return;

            var result = await GrntiEditHandler(null);
            if (result is null) return;

            _grntiRepo.Add(result);
            ReloadFromDatabase();
            SelectGrntiCommand.Execute(null);
            Status = $"Рубрика добавлена: {result.Codrub} — {result.Rubrika}";
        }

        [RelayCommand]
        private async Task EditGrntiAsync(GrntiTableRow? row)
        {
            if (GrntiEditHandler is null || row is null) return;

            var result = await GrntiEditHandler(row);
            if (result is null) return;

            _grntiRepo.Update(result);
            ReloadFromDatabase();
            SelectGrntiCommand.Execute(null);
            Status = $"Рубрика изменена: {result.Codrub} — {result.Rubrika}";
        }

        [RelayCommand]
        private async Task DeleteGrntiAsync(GrntiTableRow? row)
        {
            if (ConfirmHandler is null || row is null) return;

            var ok = await ConfirmHandler(
                "Удаление рубрики",
                $"Удалить рубрику «{row.Codrub} — {row.Rubrika}»?\nДействие необратимо.");
            if (!ok) return;

            _grntiRepo.Delete(row.Id);
            _crossValidator.RemoveInvalidExhibits(_exhibitRepo, _vuzRepo, _grntiRepo);

            ReloadFromDatabase();
            SelectGrntiCommand.Execute(null);
            Status = $"Рубрика удалена: {row.Codrub} — {row.Rubrika}";
        }

        [RelayCommand]
        private async Task AddVuzAsync()
        {
            if (VuzEditHandler is null) return;
            var result = await VuzEditHandler(null);
            if (result is null) return;

            _vuzRepo.Add(result);
            ReloadFromDatabase();
            SelectVuzCommand.Execute(null);
            Status = $"ВУЗ добавлен: {result.Codvuz}";
        }

        [RelayCommand]
        private async Task EditVuzAsync(VuzTableRow? row)
        {
            if (VuzEditHandler is null || row is null) return;
            var result = await VuzEditHandler(row);
            if (result is null) return;

            _vuzRepo.Update(result);
            ReloadFromDatabase();
            SelectVuzCommand.Execute(null);
            Status = $"ВУЗ изменён: {result.Codvuz}";
        }

        [RelayCommand]
        private async Task DeleteVuzAsync(VuzTableRow? row)
        {
            if (ConfirmHandler is null || row is null) return;

            var ok = await ConfirmHandler(
                "Удаление ВУЗа",
                $"Удалить ВУЗ «{row.Codvuz} — {row.Z1}»?\n" +
                $"Все связанные выставки этого ВУЗа также будут удалены.");
            if (!ok) return;

            _vuzRepo.Delete(row.Id);
            _crossValidator.RemoveInvalidExhibits(_exhibitRepo, _vuzRepo, _grntiRepo);

            ReloadFromDatabase();
            SelectVuzCommand.Execute(null);
            Status = $"ВУЗ удалён: {row.Codvuz}";
        }

        [RelayCommand]
        private async Task AddExhibitAsync()
        {
            if (ExhibitEditHandler is null) return;
            var result = await ExhibitEditHandler(null);
            if (result is null) return;

            _exhibitRepo.Add(result);
            ReloadFromDatabase();
            SelectExhibitsCommand.Execute(null);
            Status = $"Выставка с регистрационным номером {result.Regnumber} добавлена";
        }

        [RelayCommand]
        private async Task EditExhibitAsync(ExhibitTableRow? row)
        {
            if (ExhibitEditHandler is null || row is null) return;
            var result = await ExhibitEditHandler(row);
            if (result is null) return;

            _exhibitRepo.Update(result);
            ReloadFromDatabase();
            SelectExhibitsCommand.Execute(null);
            Status = $"Выставка с регистрационным номером {result.Regnumber} изменена";
        }

        [RelayCommand]
        private async Task DeleteExhibitAsync(ExhibitTableRow? row)
        {
            //WarningCount = 0;
            if (ConfirmHandler is null || row is null) return;

            var ok = await ConfirmHandler(
                "Удаление выставки",
                $"Удалить выставку с регистрационным номером {row.Regnumber} «{row.Subject}»?\nДействие необратимо.");
            if (!ok) return;

            _exhibitRepo.Delete(row.Id);

            ReloadFromDatabase();
            SelectExhibitsCommand.Execute(null);
            Status = $"Выставка с регистрационным номером {row.Regnumber} удалена";
        }

        private void CloseErrorScreenIfOpen()
        {
            if (!ShowErrorScreen) return;
            ShowErrorScreen = false;
            ErrorScreenText = string.Empty; // освобождаем текст из памяти
        }

        private string BuildErrorScreenText()
        {
            var sb = new StringBuilder();
            foreach (var e in _logProvider.Entries)
            {
                if (e.Level < LogLevel.Warning) continue;
                sb.AppendLine($"{e.Message}");
            }
            return sb.ToString();
        }

        [RelayCommand]
        private void GoToTables() => SelectExhibitsCommand.Execute(null);
    }
}


public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
{
    public string Status => IsValid ? "OK" : "Error";

    public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
}
