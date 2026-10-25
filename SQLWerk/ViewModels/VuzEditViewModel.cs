using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Models;
using SQLWerk.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.ViewModels;

public partial class VuzEditViewModel : ObservableObject
{
    public static List<string> RegionOptions { get; set; } = new();
    public static List<string> CityOptions { get; set; } = new();
    public static List<string> OblNameOptions { get; set; } = new();
    public static List<string> GrVedOptions { get; set; } = new();
    public static List<string> ProfOptions { get; set; } = new();
    public static List<string> StatusOptions { get; set; } = new();
    public static List<string> OblOptions { get; set; } = new();
    public static Dictionary<string, string> OblToName { get; set; } = new();

    public static HashSet<int> ExistingCodvuz { get; set; } = new();
    public static HashSet<string> ExistingZ1 { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public static HashSet<string> ExistingZ1Full { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private string _title = "Добавление ВУЗа";
    [ObservableProperty] private string _error = "";

    [ObservableProperty] private string _codvuz = "";
    [ObservableProperty] private string _z1 = "";
    [ObservableProperty] private string _z1Full = "";
    [ObservableProperty] private string _z2 = "";
    [ObservableProperty] private string _region = "";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private string _obl = "";
    [ObservableProperty] private string _oblName = "";
    [ObservableProperty] private string _grVed = "";
    [ObservableProperty] private string _prof = "";

    public bool IsEditMode { get; private set; }
    public bool IsAddMode => !IsEditMode;
    public long Id { get; private set; }
    public VuzTableRow? Result { get; private set; }
    public event EventHandler? CloseRequested;

    private int _oldCodvuz;
    private string _oldZ1 = "";
    private string _oldZ1Full = "";

    public VuzEditViewModel() { }

    partial void OnZ1Changed(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value) Z1 = filtered;
    }

    partial void OnZ1FullChanged(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value) Z1Full = filtered;
    }

    partial void OnZ2Changed(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value) Z2 = filtered;
    }

    partial void OnRegionChanged(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value) Region = filtered;
    }

    partial void OnCityChanged(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value) City = filtered;
    }

    partial void OnOblNameChanged(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value)
        {
            OblName = filtered;
            return;
        }

        var match = OblToName.FirstOrDefault(pair =>
            string.Equals(pair.Value, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match.Key != null && Obl != match.Key)
            Obl = match.Key;
    }

    partial void OnStatusChanged(string value)
    {
        var filtered = RemoveDigits(value);
        if (filtered != value) Status = filtered;
    }

    private static string RemoveDigits(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        return new string(value.Where(c => !char.IsDigit(c)).ToArray());
    }
    public VuzEditViewModel(VuzTableRow row)
    {
        IsEditMode = true;
        _title = "Изменение ВУЗа";
        Id = row.Id;
        _oldCodvuz = row.Codvuz;
        _oldZ1 = row.Z1 ?? "";
        _oldZ1Full = row.Z1Full ?? "";

        _codvuz = row.Codvuz.ToString();
        _z1 = row.Z1 ?? "";
        _z1Full = row.Z1Full ?? "";
        _z2 = row.Z2 ?? "";
        _region = row.Region ?? "";
        _city = row.City ?? "";
        _status = row.Status ?? "";
        _obl = row.Obl ?? "";
        _oblName = row.OblName ?? "";
        _grVed = row.GrVed ?? "";
        _prof = row.Prof ?? "";
    }

    partial void OnOblChanged(string oldValue, string newValue)
    {
        if (OblToName.TryGetValue((newValue ?? "").Trim(), out var name))
            OblName = name;
        else if (OblToName.TryGetValue((oldValue ?? "").Trim(), out var oldName)
                 && string.Equals(OblName, oldName, StringComparison.OrdinalIgnoreCase))
            OblName = "";
    }

    [RelayCommand]
    private void Save()
    {
        var issues = new List<string>();

        if (!int.TryParse(Codvuz?.Trim(), out var code) || code <= 0)
            issues.Add("Код ВУЗа должен быть целым числом от 1 до 2147483647");
        if (string.IsNullOrWhiteSpace(Z1)) issues.Add("Не заполнено наименование");
        if (string.IsNullOrWhiteSpace(Z1Full)) issues.Add("Не заполнено полное наименование");
        if (string.IsNullOrWhiteSpace(Z2)) issues.Add("Не заполнено краткое наименование");
        if (string.IsNullOrWhiteSpace(Region)) issues.Add("Не выбран федеральный округ");
        if (string.IsNullOrWhiteSpace(City)) issues.Add("Не заполнен город");
        if (string.IsNullOrWhiteSpace(Status)) issues.Add("Не выбран тип учреждения");
        if (string.IsNullOrWhiteSpace(Obl) || !Obl.All(char.IsDigit))
            issues.Add("Код региона должен содержать только цифры");
        if (string.IsNullOrWhiteSpace(OblName))
            issues.Add("Не заполнено название региона");

        if (code > 0 && ExistingCodvuz.Contains(code) && code != _oldCodvuz)
            issues.Add($"Код ВУЗа '{code}' уже существует");

        var z1 = (Z1 ?? "").Trim();
        if (z1.Length > 0
            && ExistingZ1.Contains(z1)
            && !string.Equals(z1, _oldZ1, StringComparison.OrdinalIgnoreCase))
            issues.Add($"Наименование '{z1}' уже существует");

        var z1f = (Z1Full ?? "").Trim();
        if (z1f.Length > 0
            && ExistingZ1Full.Contains(z1f)
            && !string.Equals(z1f, _oldZ1Full, StringComparison.OrdinalIgnoreCase))
            issues.Add($"Полное наименование '{z1f}' уже существует");

        if (issues.Count > 0)
        {
            Error = string.Join("; ", issues);
            return;
        }

        var row = new VuzTableRow
        {
            Id = Id,
            Codvuz = code,
            Z1 = z1,
            Z1Full = z1f,
            Z2 = (Z2 ?? "").Trim(),
            Region = (Region ?? "").Trim(),
            City = (City ?? "").Trim(),
            Status = (Status ?? "").Trim(),
            Obl = (Obl ?? "").Trim(),
            OblName = (OblName ?? "").Trim(),
            GrVed = string.IsNullOrWhiteSpace(GrVed) ? null : GrVed.Trim(),
            Prof = string.IsNullOrWhiteSpace(Prof) ? null : Prof.Trim(),
        };

        var (ok, reason) = ValidationService.Validate(row);
        if (!ok) { Error = reason ?? "Ошибка валидации"; return; }

        Result = row;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
