using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SQLWerk.ViewModels;

public partial class GrntiEditViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Добавление рубрики";
    [ObservableProperty] private string _codrub = "";
    [ObservableProperty] private string _rubrika = "";
    [ObservableProperty] private string _error = "";

    public long Id { get; private set; }

    
    public GrntiTableRow? Result { get; private set; }

    public event EventHandler? CloseRequested;

    public GrntiEditViewModel() {  }

    public GrntiEditViewModel(GrntiTableRow row)
    {
        _title = "Изменение рубрики";
        Id = row.Id;
        _codrub = row.Codrub ?? "";
        _rubrika = row.Rubrika ?? "";
    }

    [RelayCommand]
    private void Save()
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(Codrub)) issues.Add("Код рубрики обязателен");
        if (string.IsNullOrWhiteSpace(Rubrika)) issues.Add("Наименование рубрики обязательно");

        if (issues.Count > 0)
        {
            Error = string.Join("; ", issues);
            return;
        }

        Result = new GrntiTableRow
        {
            Id = Id,
            Codrub = Codrub.Trim(),
            Rubrika = Rubrika.Trim()
        };
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}