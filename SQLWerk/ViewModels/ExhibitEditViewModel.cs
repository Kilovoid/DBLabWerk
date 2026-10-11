using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Models;
using SQLWerk.Services;
using System;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SQLWerk.ViewModels;

public partial class ExhibitEditViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Добавление выставки";
    [ObservableProperty] private string _error = "";

    [ObservableProperty] private string _codvuz = "";
    [ObservableProperty] private string _z2 = "";
    [ObservableProperty] private string _type = "";
    [ObservableProperty] private string _regnumber = "";
    [ObservableProperty] private string _subject = "";
    [ObservableProperty] private string _grnti = "";
    [ObservableProperty] private string _bossname = "";
    [ObservableProperty] private string _bosstitle = "";
    [ObservableProperty] private string _exhitype = "";
    [ObservableProperty] private string _vystavki = "";
    [ObservableProperty] private string _exponat = "";

    public long Id { get; private set; }
    public ExhibitTableRow? Result { get; private set; }
    public event EventHandler? CloseRequested;

    public ExhibitEditViewModel() { }

    public ExhibitEditViewModel(ExhibitTableRow row)
    {
        _title = "Изменение выставки";
        Id = row.Id;
        _codvuz = row.Codvuz ?? "";
        _z2 = row.Z2 ?? "";
        _type = row.Type ?? "";
        _regnumber = row.Regnumber ?? "";
        _subject = row.Subject ?? "";
        _grnti = row.Grnti ?? "";
        _bossname = row.Bossname ?? "";
        _bosstitle = row.Bosstitle ?? "";
        _exhitype = row.Exhitype ?? "";
        _vystavki = row.Vystavki ?? "";
        _exponat = row.Exponat ?? "";
    }

    [RelayCommand]
    private void Save()
    {
        var row = new ExhibitTableRow
        {
            Id = Id,
            Codvuz = Codvuz.Trim(),
            Z2 = Z2.Trim(),
            Type = Type.Trim(),
            Regnumber = Regnumber.Trim(),
            Subject = Subject.Trim(),
            Grnti = GrntiService.ParseGrnti(Grnti),
            Bossname = Bossname.Trim(),
            Bosstitle = Bosstitle.Trim(),
            Exhitype = Exhitype.Trim(),
            Vystavki = Vystavki.Trim(),
            Exponat = Exponat.Trim(),
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
