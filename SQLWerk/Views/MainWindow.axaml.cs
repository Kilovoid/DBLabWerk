using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
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
            vm.ShowLogRequested += OnShowLog;
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
}