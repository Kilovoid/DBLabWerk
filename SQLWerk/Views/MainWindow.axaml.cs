using System;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SQLWerk.Models;
using SQLWerk.ViewModels;

namespace SQLWerk.Views;

public partial class MainWindow : Window
{
    private LogWindow? _logWindow;

    public MainWindow() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowLogRequested += OnShowLog;
            vm.SelectNirGroupsHandler = ShowNirGroupPickerAsync;
            vm.GrntiEditHandler = ShowGrntiEditAsync;
            vm.ConfirmHandler = ShowConfirmAsync;
            vm.VuzEditHandler = ShowVuzEditAsync;
            vm.ExhibitEditHandler = ShowExhibitEditAsync;
        }
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
