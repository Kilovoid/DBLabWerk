using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQLWerk.ViewModels;

public partial class GrntiEditViewModel : ObservableObject
{
    public static HashSet<string> ExistingCodrub { get; set; } = new(StringComparer.Ordinal);

    [ObservableProperty] private string _title = "Добавление рубрики";
    [ObservableProperty] private string _codrub = "";
    [ObservableProperty] private string _rubrika = "";
    [ObservableProperty] private string _error = "";

    public bool IsEditMode { get; private set; }
    public long Id { get; private set; }
    public GrntiTableRow? Result { get; private set; }
    public event EventHandler? CloseRequested;

    private string _oldCodrub = "";

    public GrntiEditViewModel() { }

    public GrntiEditViewModel(GrntiTableRow row)
    {
        IsEditMode = true;
        _title = "Изменение рубрики";
        Id = row.Id;
        _oldCodrub = row.Codrub ?? "";
        _codrub = row.Codrub ?? "";
        _rubrika = row.Rubrika ?? "";
    }

    [RelayCommand]
    private void Save()
    {
        var issues = new List<string>();

        var cod = (Codrub ?? "").Trim();
        var rub = (Rubrika ?? "").Trim();

        if (cod.Length == 0) issues.Add("Не заполнен код рубрики");
        else if (!Regex.IsMatch(cod, @"^\d{2}$"))
            issues.Add("Код рубрики должен состоять из двух цифр");

        if (rub.Length == 0) issues.Add("Не заполнено наименование рубрики");

        if (cod.Length > 0
            && ExistingCodrub.Contains(cod)
            && !string.Equals(cod, _oldCodrub, StringComparison.Ordinal))
            issues.Add($"Код рубрики '{cod}' уже существует");

        if (issues.Count > 0)
        {
            Error = string.Join("; ", issues);
            return;
        }

        Result = new GrntiTableRow
        {
            Id = Id,
            Codrub = cod,
            Rubrika = rub
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