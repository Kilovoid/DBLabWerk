using System;
using System.Threading.Tasks;
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
            vm.GrntiEditHandler = ShowGrntiEditAsync;
            vm.ConfirmHandler = ShowConfirmAsync;
            vm.VuzEditHandler = ShowVuzEditAsync;
            vm.ExhibitEditHandler = ShowExhibitEditAsync;
        }
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