using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using SQLWerk.Services;

namespace SQLWerk.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly InMemoryLoggerProvider _provider;

    [ObservableProperty] private ObservableCollection<LogEntry> _entries = new();
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _summary = "";

    public LogViewModel(InMemoryLoggerProvider provider)
    {
        _provider = provider;
        _provider.Changed += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        var sb = new StringBuilder();
        foreach (var e in _provider.Entries)
            sb.AppendLine($"{e.Time:HH:mm:ss} [{e.Level}] {e.Message}");

        LogText = sb.ToString();
        Summary = $"Записей: {_provider.Entries.Count}";
    }

    [RelayCommand]
    private void Clear() => _provider.Clear();
}
