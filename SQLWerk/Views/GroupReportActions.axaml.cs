using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SQLWerk.Models;
using SQLWerk.Services;
using SQLWerk.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SQLWerk.Views;

public partial class GroupReportActions : UserControl
{
    private readonly Dictionary<string, string> _saved = new(StringComparer.Ordinal);
    private MainWindowViewModel? _vm;
    private bool _busy;
    private ReportPreferences? _preferences;

    public GroupReportActions() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= OnGroupChanged;
        base.OnDataContextChanged(e);
        _vm = DataContext as MainWindowViewModel;
        if (_vm != null) _vm.PropertyChanged += OnGroupChanged;
        RefreshButtons();
    }

    private void OnGroupChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedNirGroup)) RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (this.FindControl<Button>("SaveReportButton") is { } save)
            save.IsEnabled = !_busy && _vm?.SelectedNirGroup != null;
        if (this.FindControl<Button>("PrintReportButton") is { } print)
            print.IsEnabled = !_busy && _vm?.SelectedNirGroup is { } group
                && _saved.TryGetValue(group.Code, out var path) && File.Exists(path);
    }

    private async void OnSaveExcel(object? sender, RoutedEventArgs e) => await SaveReportAsync("xlsx");
    private async void OnSavePdf(object? sender, RoutedEventArgs e) => await SaveReportAsync("pdf");

    private async Task SaveReportAsync(string extension)
    {
        this.FindControl<Button>("SaveReportButton")?.Flyout?.Hide();
        if (_busy || _vm?.SelectedNirGroup is not { } group || TopLevel.GetTopLevel(this) is not { } owner) return;
        var vm = _vm;
        _busy = true;
        RefreshButtons();
        try
        {
            var report = FormationReport.Capture(group.Code, vm.GroupRubrics,
                vm.GroupRegions, vm.GroupDistricts, vm.NirGroupRows);
            _preferences ??= new ReportPreferences();
            var state = _preferences.Read();
            var directory = Directory.Exists(state.Directory) ? state.Directory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            IStorageFolder? folder = null;
            if (Directory.Exists(directory))
                folder = await owner.StorageProvider.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(directory)));
            using var initialFolder = folder;
            // Always ask, even when the previous directory is known.
            using var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить отчёт", SuggestedStartLocation = initialFolder,
                SuggestedFileName = $"Отчёт {state.NextNumber} {report.CreatedAt:dd.MM.yyyy}.{extension}",
                DefaultExtension = extension, ShowOverwritePrompt = true,
                FileTypeChoices = new[] { new FilePickerFileType(extension == "pdf" ? "PDF" : "Excel")
                    { Patterns = new[] { $"*.{extension}" } } }
            });
            if (file == null) return;
            var path = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Выберите папку на компьютере для сохранения отчёта.");
            if (!Path.GetExtension(path).Equals($".{extension}", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Имя файла должно заканчиваться на .{extension}.");
            path = Path.GetFullPath(path);
            await Task.Run(() =>
            {
                if (extension == "pdf") ReportPdfFallback.Save(path, report);
                else FormationReportWriter.Save(path, report);
            });
            _saved[report.Group] = path;
            try { _preferences.Remember(state.NextNumber, Path.GetDirectoryName(path)!); }
            catch (Exception ex)
            {
                vm.NirGroupMessage = $"Отчёт сохранён: {path}. Не удалось запомнить номер и папку: {ex.Message}";
                return;
            }
            vm.NirGroupMessage = $"Отчёт сохранён: {path}";
        }
        catch (Exception ex) { vm.NirGroupMessage = $"Не удалось сохранить отчёт: {ex.Message}"; }
        finally { _busy = false; RefreshButtons(); }
    }

    private async Task ShowPdfWarningAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var close = new Button { Content = "ОК", IsDefault = true,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var dialog = new Window
        {
            Title = "Печать через PDF", Width = 460, SizeToContent = SizeToContent.Height,
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20), Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Microsoft Excel не установлен или не смог открыть печать. Будет сформирован PDF и открыт его предпросмотр.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    close
                }
            }
        };
        close.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner);
    }

    private async void OnPrintReport(object? sender, RoutedEventArgs e)
    {
        if (_busy || _vm?.SelectedNirGroup is not { } group || !_saved.TryGetValue(group.Code, out var path)) return;
        var vm = _vm;
        _busy = true;
        RefreshButtons();
        try
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Сохранённый отчёт не найден. Сохраните его заново.");
            if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                vm.NirGroupMessage = ReportPdfFallback.OpenPdf(path);
            else
                vm.NirGroupMessage = await ExcelReportPrinter.OpenAsync(path, ShowPdfWarningAsync);
        }
        catch (Exception ex) { vm.NirGroupMessage = $"Не удалось открыть печать: {ex.Message}"; }
        finally { _busy = false; RefreshButtons(); }
    }
}
