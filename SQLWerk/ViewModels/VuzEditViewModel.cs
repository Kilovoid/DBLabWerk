using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using SQLWerk.Models;
using SQLWerk.Services;
using System;
using System.Net.NetworkInformation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SQLWerk.ViewModels;

public partial class VuzEditViewModel : ObservableObject
{
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

    public long Id { get; private set; }
    public VuzTableRow? Result { get; private set; }
    public event EventHandler? CloseRequested;

    public VuzEditViewModel() { }

    public VuzEditViewModel(VuzTableRow row)
    {
        _title = "Изменение ВУЗа";
        Id = row.Id;
        _codvuz = row.Codvuz ?? "";
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

    [RelayCommand]
    private void Save()
    {
        var row = new VuzTableRow
        {
            Id = Id,
            Codvuz = Codvuz.Trim(),
            Z1 = Z1.Trim(),
            Z1Full = Z1Full.Trim(),
            Z2 = Z2.Trim(),
            Region = Region.Trim(),
            City = City.Trim(),
            Status = Status.Trim(),
            Obl = Obl.Trim(),
            OblName = OblName.Trim(),
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