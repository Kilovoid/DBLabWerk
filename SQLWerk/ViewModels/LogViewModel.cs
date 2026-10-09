using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Services;

namespace SQLWerk.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly InMemoryLoggerProvider _provider;

    [ObservableProperty] private ObservableCollection<LogEntry> _entries = new();
    [ObservableProperty] private string _summary = "";

    public LogViewModel(InMemoryLoggerProvider provider)
    {
        _provider = provider;
        _provider.Changed += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        Entries = new ObservableCollection<LogEntry>(_provider.Entries);
        Summary = $"Записей: {Entries.Count}";
    }

    [RelayCommand]
    private void Clear() => _provider.Clear();
}
