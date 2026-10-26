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
        private readonly IBatchDeleteService _batchDelete;
        private readonly INirGroupRepository _nirGroupRepo;

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
        [ObservableProperty] private bool _showNirGroups;
        [ObservableProperty] private bool _showErrorScreen;
        [ObservableProperty] private string _errorScreenText = "";

        [ObservableProperty] private bool _isBlinking;

        public MultiSelectFilterViewModel RegionFilter { get; } = new("Федеральный округ");
        public MultiSelectFilterViewModel OblFilter { get; } = new("Субъект федерации");
        public MultiSelectFilterViewModel CityFilter { get; } = new("Город");
        public MultiSelectFilterViewModel GrntiFilter { get; } = new("Рубрика ГРНТИ");
        public MultiSelectFilterViewModel ExhitypeFilter { get; } = new("Наличие экспоната");
        public MultiSelectFilterViewModel TypeFilter { get; } = new("Признак формы");
        private bool _updatingFilters;

        private IEnumerable<MultiSelectFilterViewModel> Filters =>
            new[] { RegionFilter, OblFilter, CityFilter, GrntiFilter, ExhitypeFilter, TypeFilter };

        [ObservableProperty] private ObservableCollection<FullModel> _filteredFull = new();




        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditExhibitCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteExhibitCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddSelectedExhibitsToNirGroupCommand))]
        private List<ExhibitTableRow> _selectedExhibits = new();

        private bool CanEditExhibit() => SelectedExhibits.Count == 1;
        private bool CanDeleteExhibit() => SelectedExhibits.Count > 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditVuzCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVuzCommand))]
        private List<VuzTableRow> _selectedVuzes = new();

        private bool CanEditVuz() => SelectedVuzes.Count == 1;
        private bool CanDeleteVuz() => SelectedVuzes.Count > 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditGrntiCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteGrntiCommand))]
        private List<GrntiTableRow> _selectedGrntiRows = new();

        private bool CanEditGrnti() => SelectedGrntiRows.Count == 1;
        private bool CanDeleteGrnti() => SelectedGrntiRows.Count > 0;

        [ObservableProperty] private ObservableCollection<NirGroup> _nirGroups = new();
        [ObservableProperty] private ObservableCollection<NirGroupEntry> _nirGroupRows = new();
        [ObservableProperty] private string _newNirGroupName = "";
        [ObservableProperty] private string _nirGroupMessage = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteNirGroupCommand))]
        private NirGroup? _selectedNirGroup;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddToNirGroupCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditFullExhibitCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteFullExhibitsCommand))]
        private List<FullModel> _selectedFullRows = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveFromNirGroupCommand))]
        private List<NirGroupEntry> _selectedGroupRows = new();

        public Func<Task<string[]?>>? SelectNirGroupsHandler { get; set; }
        private bool CanAddToNirGroup() => SelectedFullRows.Count > 0;
        private bool CanAddSelectedExhibitsToNirGroup() => SelectedExhibits.Count > 0;
        private bool CanEditFullExhibit() => SelectedFullRows.Count == 1;
        private bool CanDeleteFullExhibits() => SelectedFullRows.Count > 0;
        private bool CanRemoveFromNirGroup() => SelectedGroupRows.Count > 0;

        partial void OnSelectedNirGroupChanged(NirGroup? value) => RefreshNirGroup();

        [RelayCommand]
        private void ShowNirGroup()
        {
            CloseErrorScreenIfOpen();
            ShowExhibits = ShowVuz = ShowGrnti = ShowAll = false;
            ShowNirGroups = true;
            ReloadNirGroups();
            RowCount = NirGroupRows.Count;
            Status = "Группы НИР: выберите группу по её названию.";
        }

        [RelayCommand]
        private void CreateNirGroup()
        {
            if (TryCreateNirGroup(NewNirGroupName, out var message))
                NewNirGroupName = "";
            NirGroupMessage = message;
        }

        private bool CanDeleteNirGroup() => SelectedNirGroup != null;

        [RelayCommand(CanExecute = nameof(CanDeleteNirGroup))]
        private async Task DeleteNirGroupAsync()
        {
            if (SelectedNirGroup is not { } group || ConfirmHandler is null) return;
            try
            {
                int count = _nirGroupRepo.GetExhibitIds(group.Code).Count;
                bool confirmed = await ConfirmHandler("Удаление группы НИР",
                    $"Удалить группу «{group.Code}»?\n\n" +
                    $"Выставок в группе: {count}.");
                if (!confirmed) return;

                bool deleted = _nirGroupRepo.DeleteGroup(group.Code);
                SelectedNirGroup = null;
                ReloadNirGroups();
                Status = NirGroupMessage = deleted
                    ? $"Группа «{group.Code}» удалена."
                    : "Группа уже была удалена. Список обновлён.";
            }
            catch (Exception ex)
            {
                Status = NirGroupMessage = "Ошибка удаления группы: " + ex.Message;
            }
        }

        // Один путь проверки и сохранения для раздела групп и диалога «Новая…».
        public bool TryCreateNirGroup(string name, out string message)
        {
            var code = (name ?? "").Trim();
            if (code.Length == 0)
            {
                message = "Введите название группы.";
                return false;
            }
            if (NirGroups.Any(group => string.Equals(group.Code, code, StringComparison.Ordinal)))
            {
                message = "Группа с таким названием уже существует.";
                return false;
            }
            try
            {
                _nirGroupRepo.CreateGroup(code);
                ReloadNirGroups(code);
                message = $"Группа «{code}» создана.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Не удалось создать группу: " + ex.Message;
                return false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddToNirGroup))]
        private async Task AddToNirGroup()
        {
            var ids = SelectedFullRows.Select(row => row.Id).Distinct().ToArray();
            await AddExhibitsToGroupAsync(ids);
        }

        [RelayCommand(CanExecute = nameof(CanAddSelectedExhibitsToNirGroup))]
        private async Task AddSelectedExhibitsToNirGroup()
        {
            var ids = SelectedExhibits.Select(row => row.Id).Distinct().ToArray();
            await AddExhibitsToGroupAsync(ids);
        }

        private async Task AddExhibitsToGroupAsync(long[] ids)
        {
            if (ids.Length == 0 || SelectNirGroupsHandler is null) return;
            try
            {
                var selected = await SelectNirGroupsHandler();
                if (selected is null || selected.Length == 0) return;
                var codes = selected.Distinct(StringComparer.Ordinal).ToArray();
                int added = _nirGroupRepo.AddMembers(codes, ids);
                ReloadNirGroups();
                Status = NirGroupMessage = $"";
            }
            catch (Exception ex) { Status = NirGroupMessage = "Ошибка добавления: " + ex.Message; }
        }

        [RelayCommand(CanExecute = nameof(CanEditFullExhibit))]
        private async Task EditFullExhibit()
        {
            if (!CanEditFullExhibit()) return;
            var row = Exhibits.FirstOrDefault(item => item.Id == SelectedFullRows[0].Id);
            if (row is null) return;
            SelectedExhibits = new() { row };
            await EditExhibitAsync();
            var status = Status;
            SelectFull();
            Status = status;
        }

        [RelayCommand(CanExecute = nameof(CanDeleteFullExhibits))]
        private async Task DeleteFullExhibits()
        {
            if (!CanDeleteFullExhibits()) return;
            var ids = SelectedFullRows.Select(row => row.Id).ToHashSet();
            SelectedExhibits = Exhibits.Where(row => ids.Contains(row.Id)).ToList();
            await DeleteExhibitAsync();
            var status = Status;
            SelectFull();
            Status = status;
        }

        [RelayCommand(CanExecute = nameof(CanRemoveFromNirGroup))]
        private void RemoveFromNirGroup()
        {
            if (!CanRemoveFromNirGroup() || SelectedNirGroup is not { } group) return;
            var ids = SelectedGroupRows.Select(row => row.Exhibit.Id).Distinct().ToArray();
            try
            {
                int removed = _nirGroupRepo.RemoveMembers(group.Code, ids);
                RefreshNirGroup();
                Status = NirGroupMessage = $"Из группы исключено: {removed}.";
            }
            catch (Exception ex) { RefreshNirGroup(); NirGroupMessage = "Ошибка исключения: " + ex.Message; }
        }

        private void ReloadNirGroups(string? preferredCode = null)
        {
            var code = preferredCode ?? SelectedNirGroup?.Code;
            _nirGroupRepo.EnsureCreated();
            NirGroups = new ObservableCollection<NirGroup>(_nirGroupRepo.GetGroups());
            SelectedNirGroup = NirGroups.FirstOrDefault(group => group.Code == code);
            RefreshNirGroup();
        }

        private void RefreshNirGroup()
        {
            SelectedGroupRows = new();
            if (SelectedNirGroup is not { } group)
            {
                NirGroupRows = new();
            }
            else
            {
                var ids = _nirGroupRepo.GetExhibitIds(group.Code).ToHashSet();
                NirGroupRows = new ObservableCollection<NirGroupEntry>(FullTable
                    .Where(row => ids.Contains(row.Id)).DistinctBy(row => row.Id)
                    .Select(row => new NirGroupEntry { GroupCode = group.Code, Exhibit = row }));
            }
            if (ShowNirGroups)
            {
                RowCount = NirGroupRows.Count;
                Status = SelectedNirGroup is null ? "Выберите или создайте группу НИР."
                    : $"Группа «{SelectedNirGroup.Code}»: {RowCount} строк";
            }
        }

        public event EventHandler? ShowLogRequested;
        public Func<GrntiTableRow?, Task<GrntiTableRow?>>? GrntiEditHandler { get; set; }
        public Func<string, string, Task<bool>>? ConfirmHandler { get; set; }
        public Func<VuzTableRow?, Task<VuzTableRow?>>? VuzEditHandler { get; set; }
        public Func<ExhibitTableRow?, Task<ExhibitTableRow?>>? ExhibitEditHandler { get; set; }

        private void ApplyFiltersAndRefreshStatus()
        {
            if (_updatingFilters) return;
            ApplyFilters();
            if (ShowAll)
            {
                RowCount = FilteredFull.Count;
                Status = $"Вся информация (фильтр): {RowCount} строк";
            }
        }

        private static IEnumerable<string> GetMainRubrics(string? value) => (value ?? "")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.Trim())
            .Where(code => code.Length >= 2)
            .Select(code => code.Substring(0, 2))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        private void ApplyFilters()
        {
            var regions = RegionFilter.GetSelection();
            var oblasts = OblFilter.GetSelection();
            var cities = CityFilter.GetSelection();
            var rubrics = GrntiFilter.GetSelection();
            var exhibitTypes = ExhitypeFilter.GetSelection();
            var types = TypeFilter.GetSelection();

            static bool Matches(HashSet<string>? selected, string? value) =>
                selected == null || selected.Contains((value ?? "").Trim());

            FilteredFull = new ObservableCollection<FullModel>(FullTable.Where(row =>
                Matches(regions, row.Region)
                && Matches(oblasts, row.OblName)
                && Matches(cities, row.City)
                && Matches(exhibitTypes, row.Exhitype)
                && Matches(types, row.Type)
                && (rubrics == null || GetMainRubrics(row.Grnti)
                    .DefaultIfEmpty("").Any(rubrics.Contains))));
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
            CrossValidationService crossValidator,
            IBatchDeleteService batchDelete,
            INirGroupRepository nirGroupRepo)
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
            _batchDelete = batchDelete;
            _nirGroupRepo = nirGroupRepo;

            foreach (var filter in Filters)
                filter.SelectionChanged += (_, _) => ApplyFiltersAndRefreshStatus();


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
            ReloadFromDatabase();
            if (haveData)
            {
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
                .Select(v => v.Codvuz)
                .Where(c => c > 0)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => c.ToString())
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

            VuzEditViewModel.CityOptions = Options(Vuzes.Select(v => v.City));
            VuzEditViewModel.OblNameOptions = Options(map.Values.Concat(Vuzes.Select(v => v.OblName)));
            VuzEditViewModel.GrVedOptions = Options(Vuzes.Select(v => v.GrVed));
            VuzEditViewModel.ProfOptions = Options(Vuzes.Select(v => v.Prof));

            VuzEditViewModel.ExistingCodvuz = Vuzes
                .Select(v => v.Codvuz)
                .Where(code => code > 0)
                .ToHashSet();

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

            ReloadNirGroups();
            RefreshFilterOptions();
            ApplyFilters();
            //SelectExhibitsCommand.Execute(null);
        }
        private static List<string> Options(IEnumerable<string?> values) => values
            .Select(value => (value ?? "").Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

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
            ShowNirGroups = false;
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
            RegionFilter.SetValues(FullTable.Select(row => row.Region));
            OblFilter.SetValues(FullTable.Select(row => row.OblName));
            CityFilter.SetValues(FullTable.Select(row => row.City));
            GrntiFilter.SetValues(FullTable.SelectMany(row =>
                GetMainRubrics(row.Grnti).DefaultIfEmpty("")));
            ExhitypeFilter.SetValues(FullTable.Select(row => row.Exhitype));
            TypeFilter.SetValues(FullTable.Select(row => row.Type));
        }

        [RelayCommand]
        private void ResetFilters()
        {
            _updatingFilters = true;
            try
            {
                foreach (var filter in Filters)
                    filter.Reset();
            }
            finally { _updatingFilters = false; }
            ApplyFiltersAndRefreshStatus();
        }

        [RelayCommand]
        public void SelectVuz()
        {
            ShowNirGroups = false;
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
            ShowNirGroups = false;
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
            ShowNirGroups = false;
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

        [RelayCommand(CanExecute = nameof(CanEditGrnti))]
        private async Task EditGrntiAsync()
        {
            if (GrntiEditHandler is null || !CanEditGrnti()) return;
            var row = SelectedGrntiRows[0];

            var result = await GrntiEditHandler(row);
            if (result is null) return;

            _grntiRepo.Update(result);
            ReloadFromDatabase();
            SelectGrntiCommand.Execute(null);
            Status = $"Рубрика изменена: {result.Codrub} — {result.Rubrika}";
        }

        [RelayCommand(CanExecute = nameof(CanDeleteGrnti))]
        private async Task DeleteGrntiAsync()
        {
            var rows = SelectedGrntiRows.DistinctBy(row => row.Id).ToList();
            if (rows.Count == 0) return;
            var ids = rows.Select(row => row.Id).ToArray();
            await DeleteSelectedAsync(
                "Удаление рубрик ГРНТИ", rows.Select(row => $"{row.Codrub} — {row.Rubrika}").ToList(),
                "Все выставки, содержащие хотя бы одну выбранную рубрику ГРНТИ, также будут удалены.",
                () => _batchDelete.DeleteGrnti(ids), SelectGrnti);
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

        [RelayCommand(CanExecute = nameof(CanEditVuz))]
        private async Task EditVuzAsync()
        {
            if (VuzEditHandler is null || !CanEditVuz()) return;
            var row = SelectedVuzes[0];
            var result = await VuzEditHandler(row);
            if (result is null) return;

            _vuzRepo.Update(result);
            ReloadFromDatabase();
            SelectVuzCommand.Execute(null);
            Status = $"ВУЗ изменён: {result.Codvuz}";
        }

        [RelayCommand(CanExecute = nameof(CanDeleteVuz))]
        private async Task DeleteVuzAsync()
        {
            var rows = SelectedVuzes.DistinctBy(row => row.Id).ToList();
            if (rows.Count == 0) return;
            var ids = rows.Select(row => row.Id).ToArray();
            await DeleteSelectedAsync(
                "Удаление вузов", rows.Select(row => $"{row.Codvuz} — {row.Z1}").ToList(),
                "Все выставки, связанные с выбранными вузами, также будут удалены.",
                () => _batchDelete.DeleteVuzes(ids), SelectVuz);
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

        [RelayCommand(CanExecute = nameof(CanEditExhibit))]
        private async Task EditExhibitAsync()
        {
            if (ExhibitEditHandler is null || !CanEditExhibit()) return;
            var row = SelectedExhibits[0];
            var result = await ExhibitEditHandler(row);
            if (result is null) return;

            _exhibitRepo.Update(result);
            ReloadFromDatabase();
            SelectExhibitsCommand.Execute(null);
            Status = $"Выставка с регистрационным номером {result.Regnumber} изменена";
        }

        [RelayCommand(CanExecute = nameof(CanDeleteExhibit))]
        private async Task DeleteExhibitAsync()
        {
            var rows = SelectedExhibits.DistinctBy(row => row.Id).ToList();
            if (rows.Count == 0) return;
            var ids = rows.Select(row => row.Id).ToArray();
            await DeleteSelectedAsync(
                "Удаление выставок", rows.Select(row => $"{row.Regnumber} — {row.Subject}").ToList(),
                null,
                () => _batchDelete.DeleteExhibits(ids), SelectExhibits);
        }

        private async Task DeleteSelectedAsync(string title, List<string> labels,
            string? relatedWarning, Func<(int Deleted, int Related)> delete, Action showTable)
        {
            if (ConfirmHandler is null) return;
            var preview = string.Join("\n", labels.Take(10).Select(label => "• " + label));
            if (labels.Count > 10) preview += $"\n… и ещё {labels.Count - 10}";
            bool confirmed;
            if (labels.Count > 1)
            { 
            confirmed = await ConfirmHandler(title,
                $"Удалить выбранные записи: {labels.Count}?\n\n" +
                relatedWarning + "\nДействие необратимо.");
            }
            else
            {
                confirmed = await ConfirmHandler(title,
                $"Удалить выбранные записи: {labels.Count}?\n\n{preview}\n\n" +
                relatedWarning + "\nДействие необратимо.");
            }
            if (!confirmed) return;

            try
            {
                StartNewSession();
                var result = delete();
                ReloadFromDatabase();
                showTable();
                Status = $"Удалено записей: {result.Deleted}; связанных выставок: {result.Related}";
            }
            catch (Exception ex)
            {
                Status = "Ошибка удаления: " + ex.Message;
            }
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
