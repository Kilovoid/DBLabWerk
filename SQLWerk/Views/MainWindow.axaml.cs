using System;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using SQLWerk.Models;
using SQLWerk.ViewModels;

namespace SQLWerk.Views;

public partial class MainWindow : Window
{
    private LogWindow? _logWindow;
    private MainWindowViewModel? _boundVm;
    private bool _selectingGroupSuggestion;

    public MainWindow() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_boundVm != null)
        {
            _boundVm.ShowLogRequested -= OnShowLog;
            _boundVm.NavigateToRecordRequested -= OnNavigateToRecord;
        }
        _boundVm = null;
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            _boundVm = vm;
            vm.ShowLogRequested += OnShowLog;
            vm.NavigateToRecordRequested += OnNavigateToRecord;
            vm.SelectNirGroupsHandler = ShowNirGroupPickerAsync;
            vm.GrntiEditHandler = ShowGrntiEditAsync;
            vm.ConfirmHandler = ShowConfirmAsync;
            vm.VuzEditHandler = ShowVuzEditAsync;
            vm.ExhibitEditHandler = ShowExhibitEditAsync;
        }
    }

    private void OnNavigateToRecord(string table, long id)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not MainWindowViewModel vm) return;
            DataGrid? grid = null;
            object? item = null;
            switch (table)
            {
                case "Exhibit": grid = this.FindControl<DataGrid>("ExhibitsGrid"); item = vm.Exhibits.FirstOrDefault(r => r.Id == id); break;
                case "Vuz": grid = this.FindControl<DataGrid>("VuzGrid"); item = vm.Vuzes.FirstOrDefault(r => r.Id == id); break;
                case "Grnti": grid = this.FindControl<DataGrid>("GrntiGrid"); item = vm.Grnti.FirstOrDefault(r => r.Id == id); break;
            }
            if (grid == null || item == null) return;
            grid.SelectedItem = item;
            grid.ScrollIntoView(item, null);
            grid.Focus();
        }, DispatcherPriority.Loaded);
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not DataGrid grid) return;
        switch (grid.Name)
        {
            case "ExhibitsGrid":
                vm.SelectedExhibits = grid.SelectedItems.OfType<ExhibitTableRow>().ToList();
                break;
            case "VuzGrid":
                vm.SelectedVuzes = grid.SelectedItems.OfType<VuzTableRow>().ToList();
                break;
            case "NirGroupsGrid":
                vm.SelectedGroupRows = grid.SelectedItems.OfType<NirGroupEntry>().ToList();
                break;
            case "FullGrid":
                vm.SelectedFullRows = grid.SelectedItems.OfType<FullModel>().ToList();
                break;
            case "GrntiGrid":
                vm.SelectedGrntiRows = grid.SelectedItems.OfType<GrntiTableRow>().ToList();
                break;
        }
    }

    private void OnGroupSuggestionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_selectingGroupSuggestion || DataContext is not MainWindowViewModel vm) return;
        var group = e.AddedItems.OfType<NirGroup>().FirstOrDefault();
        if (group == null) return;
        _selectingGroupSuggestion = true;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Complete the AutoCompleteBox text update before selecting the group.
                vm.NirGroupSearch = "";
                vm.NirGroupCommentSearch = "";
                vm.SelectedNirGroup = vm.NirGroups.FirstOrDefault(g => g.Code == group.Code);
                if (sender is AutoCompleteBox input) input.IsDropDownOpen = false;
            }
            finally { _selectingGroupSuggestion = false; }
        }, DispatcherPriority.Background);
    }

    private async void OnNewNirGroupClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var button = sender as Button;
        if (button != null) button.IsEnabled = false;
        try { await new NewNirGroupWindow(vm).ShowDialog<string?>(this); }
        finally { if (button != null) button.IsEnabled = true; }
    }

    private async Task<string[]?> ShowNirGroupPickerAsync()
    {
        if (DataContext is not MainWindowViewModel vm) return null;
        return await new NirGroupPickerWindow(vm).ShowDialog<string[]?>(this);
    }

    private void OnShowLog(object? sender, EventArgs e)
    {
        if (_logWindow is { IsVisible: true })
        {
            _logWindow.Activate();
            return;
        }

        _logWindow = new LogWindow
        {
            DataContext = App.Services.GetRequiredService<LogViewModel>()
        };
        _logWindow.Closed += (_, _) => _logWindow = null;
        _logWindow.Show(this);
    }

    private async Task<GrntiTableRow?> ShowGrntiEditAsync(GrntiTableRow? existing)
    {
        var dialogVm = existing is null
            ? new GrntiEditViewModel()
            : new GrntiEditViewModel(existing);

        var dlg = new GrntiEditWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<GrntiTableRow?>(this);
    }

    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialogVm = new ConfirmViewModel(title, message);
        var dlg = new ConfirmWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<bool>(this);
    }

    private async Task<VuzTableRow?> ShowVuzEditAsync(VuzTableRow? existing)
    {
        var dialogVm = existing is null
            ? new VuzEditViewModel()
            : new VuzEditViewModel(existing);

        var dlg = new VuzEditWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<VuzTableRow?>(this);
    }

    private async Task<ExhibitTableRow?> ShowExhibitEditAsync(ExhibitTableRow? existing)
    {
        var dialogVm = existing is null
            ? new ExhibitEditViewModel()
            : new ExhibitEditViewModel(existing);

        var dlg = new ExhibitEditWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<ExhibitTableRow?>(this);
    }
}
