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

        private int _sessionStartIndex;
        private int _sessionWarningCount;
        private bool _blinkAcknowledged;

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

        [ObservableProperty] private bool _isBlinking;

        private const string AllOption = "Все";

        [ObservableProperty] private string _filterType = AllOption;
        [ObservableProperty] private string _filterRegion = AllOption;
        [ObservableProperty] private string _filterObl = AllOption;
        [ObservableProperty] private string _filterCity = AllOption;
        [ObservableProperty] private string _filterGrnti = AllOption;
        [ObservableProperty] private string _filterExhitype = AllOption;

        public ObservableCollection<string> FilterRegionOptions { get; } = new();
        public ObservableCollection<string> FilterOblOptions { get; } = new();
        public ObservableCollection<string> FilterCityOptions { get; } = new();
        public ObservableCollection<string> FilterGrntiOptions { get; } = new();
        public ObservableCollection<string> FilterExhitypeOptions { get; } = new();
        public ObservableCollection<string> FilterTypeOptions { get; } = new();

        [ObservableProperty] private ObservableCollection<FullModel> _filteredFull = new();

        public event EventHandler? ShowLogRequested;
        public Func<GrntiTableRow?, Task<GrntiTableRow?>>? GrntiEditHandler { get; set; }
        public Func<string, string, Task<bool>>? ConfirmHandler { get; set; }
        public Func<VuzTableRow?, Task<VuzTableRow?>>? VuzEditHandler { get; set; }
        public Func<ExhibitTableRow?, Task<ExhibitTableRow?>>? ExhibitEditHandler { get; set; }

        partial void OnFilterRegionChanged(string value) => ApplyFiltersAndRefreshStatus();
        partial void OnFilterOblChanged(string value) => ApplyFiltersAndRefreshStatus();
        partial void OnFilterCityChanged(string value) => ApplyFiltersAndRefreshStatus();
        partial void OnFilterGrntiChanged(string value) => ApplyFiltersAndRefreshStatus();
        partial void OnFilterExhitypeChanged(string value) => ApplyFiltersAndRefreshStatus();
        partial void OnFilterTypeChanged(string value) => ApplyFiltersAndRefreshStatus();

        private void ApplyFiltersAndRefreshStatus()
        {
            ApplyFilters();

            RowCount = FilteredFull.Count;
            Status = $"Вся информация (фильтр): {RowCount} строк";
        }

        private void ApplyFilters()
        {
            bool noFilters =
                FilterRegion == AllOption &&
                FilterObl == AllOption &&
                FilterCity == AllOption &&
                FilterGrnti == AllOption &&
                FilterExhitype == AllOption &&
                FilterType == AllOption;

            IEnumerable<FullModel> source = FullTable;

            if (!noFilters)
            {
                source = source.Where(f =>
                {
                    if (FilterRegion != AllOption
                        && !string.Equals(f.Region ?? "", FilterRegion, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (FilterObl != AllOption
                        && !string.Equals(f.OblName ?? "", FilterObl, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (FilterCity != AllOption
                        && !string.Equals(f.City ?? "", FilterCity, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (FilterExhitype != AllOption
                        && !string.Equals(f.Exhitype ?? "", FilterExhitype, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (FilterType != AllOption
                        && !string.Equals(f.Type ?? "", FilterType, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (FilterGrnti != AllOption)
                    {
                        var codes = (f.Grnti ?? "")
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim());
                        if (!codes.Any(c => c.StartsWith(FilterGrnti, StringComparison.Ordinal)))
                            return false;
                    }

                    return true;
                });
            }

            FilteredFull = new ObservableCollection<FullModel>(source);
        }


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
        private void ShowLog()
        {
            _blinkAcknowledged = true;
            UpdateBlinking();
            ShowLogRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnLogChanged(object? sender, EventArgs e)
        {
            var session = _logProvider.Entries.Skip(_sessionStartIndex).ToList();
            var newCount = session.Count(x => x.Level >= LogLevel.Warning);

            HasErrors = newCount > 0;
            WarningCount = newCount;
            _sessionWarningCount = newCount;

            UpdateBlinking();
        }

        private void StartNewSession()
        {
            _sessionStartIndex = _logProvider.Entries.Count;
            _sessionWarningCount = 0;
            _blinkAcknowledged = false;
            OnLogChanged(this, EventArgs.Empty);
        }

        private void UpdateBlinking()
        {
            IsBlinking = _sessionWarningCount > 0 && !_blinkAcknowledged;
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

            ExhibitEditViewModel.VuzCodes = Vuzes
                .Select(v => v.Codvuz ?? "")
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ExhibitEditViewModel.GrntiRubrics = Grnti
                .Select(g => g.Codrub ?? "")
                .Where(c => c.Length == 2)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            VuzEditViewModel.RegionOptions = Vuzes
                .Select(v => (v.Region ?? "").Trim())
                .Where(s => s.Length > 0)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            VuzEditViewModel.StatusOptions = Vuzes
                .Select(v => (v.Status ?? "").Trim())
                .Where(s => s.Length > 0)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var dbObl = Vuzes
                .Select(v => (v.Obl ?? "").Trim())
                .Where(s => s.Length > 0)
                .Distinct();

            VuzEditViewModel.OblOptions = RegionCodes.Codes
                .Concat(dbObl)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var map = new Dictionary<string, string>(RegionCodes.All);
            foreach (var v in Vuzes)
            {
                var code = (v.Obl ?? "").Trim();
                var name = (v.OblName ?? "").Trim();
                if (code.Length > 0 && !map.ContainsKey(code))
                    map[code] = name;
            }
            VuzEditViewModel.OblToName = map;

            VuzEditViewModel.ExistingCodvuz = Vuzes
                .Select(v => (v.Codvuz ?? "").Trim())
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            VuzEditViewModel.ExistingZ1 = Vuzes
                .Select(v => (v.Z1 ?? "").Trim())
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            VuzEditViewModel.ExistingZ1Full = Vuzes
                .Select(v => (v.Z1Full ?? "").Trim())
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            GrntiEditViewModel.ExistingCodrub = Grnti
                .Select(g => (g.Codrub ?? "").Trim())
                .Where(s => s.Length > 0)
              .ToHashSet(StringComparer.Ordinal);

            RefreshFilterOptions();
            ApplyFilters();
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

        private void RefreshFilterOptions()
        {
            var keepRegion = FilterRegion;
            var keepObl = FilterObl;
            var keepCity = FilterCity;
            var keepGrnti = FilterGrnti;
            var keepExh = FilterExhitype;
            var keepType = FilterType;

            FilterRegionOptions.Clear();
            FilterRegionOptions.Add(AllOption);
            foreach (var s in FullTable.Select(f => (f.Region ?? "").Trim())
                                        .Where(s => s.Length > 0).Distinct().OrderBy(s => s))
                FilterRegionOptions.Add(s);

            FilterOblOptions.Clear();
            FilterOblOptions.Add(AllOption);
            foreach (var s in FullTable.Select(f => (f.OblName ?? "").Trim())
                                        .Where(s => s.Length > 0).Distinct().OrderBy(s => s))
                FilterOblOptions.Add(s);

            FilterCityOptions.Clear();
            FilterCityOptions.Add(AllOption);
            foreach (var s in FullTable.Select(f => (f.City ?? "").Trim())
                                        .Where(s => s.Length > 0).Distinct().OrderBy(s => s))
                FilterCityOptions.Add(s);

            FilterGrntiOptions.Clear();
            FilterGrntiOptions.Add(AllOption);
            foreach (var s in Grnti.Select(g => (g.Codrub ?? "").Trim())
                                    .Where(s => s.Length == 2).Distinct().OrderBy(s => s))
                FilterGrntiOptions.Add(s);

            FilterExhitypeOptions.Clear();
            FilterExhitypeOptions.Add(AllOption);
            foreach (var s in ExhibitEditViewModel.ExhibitOptions)
                FilterExhitypeOptions.Add(s);

            FilterTypeOptions.Clear();
            FilterTypeOptions.Add(AllOption);
            foreach (var s in ExhibitEditViewModel.TypeOptions)
                FilterTypeOptions.Add(s);

            FilterRegion = FilterRegionOptions.Contains(keepRegion) ? keepRegion : AllOption;
            FilterObl = FilterOblOptions.Contains(keepObl) ? keepObl : AllOption;
            FilterCity = FilterCityOptions.Contains(keepCity) ? keepCity : AllOption;
            FilterGrnti = FilterGrntiOptions.Contains(keepGrnti) ? keepGrnti : AllOption;
            FilterExhitype = FilterExhitypeOptions.Contains(keepExh) ? keepExh : AllOption;
            FilterType = FilterTypeOptions.Contains(keepType) ? keepType : AllOption;
        }

        [RelayCommand]
        private void ResetFilters()
        {
            FilterRegion = AllOption;
            FilterObl = AllOption;
            FilterCity = AllOption;
            FilterGrnti = AllOption;
            FilterExhitype = AllOption;
            FilterType = AllOption;
            ApplyFiltersAndRefreshStatus();
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

            ApplyFilters();
            RowCount = FilteredFull.Count;
            Status = $"Вся информация: {RowCount} строк";
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            _logProvider.Clear();
            StartNewSession();

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
                $"Удалить рубрику «{row.Codrub} — {row.Rubrika}»?\n" +
                $"Все связи с данной рубрикой будут удалены\n" +
                $"Действие необратимо.");
            if (!ok) return;

            StartNewSession();

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

            StartNewSession();

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
            if (!ShowErrorScreen && !IsBlinking) return;

            _blinkAcknowledged = true;
            UpdateBlinking();

            ShowErrorScreen = false;
            ErrorScreenText = string.Empty;
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
