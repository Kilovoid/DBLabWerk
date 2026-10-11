using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Models;
using SQLWerk.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SQLWerk.ViewModels;

public partial class GrntiEntryVM : ObservableObject
{
    [ObservableProperty] private string _rubric = "";
    [ObservableProperty] private string _sub = "";
}

public partial class ExhibitEditViewModel : ObservableObject
{
    public static List<string> VuzCodes { get; set; } = new();
    public static List<string> GrntiRubrics { get; set; } = new();

    public static readonly string[] TypeOptions =
    {
        "Е", "М"
    };

    public static readonly string[] ExhibitOptions = { "Е", "Н", "П" };

    private int MaxGrnti = 5;

    [ObservableProperty] private string _title = "Добавление выставки";
    [ObservableProperty] private string _error = "";

    [ObservableProperty] private string _codvuz = "";
    [ObservableProperty] private string _type = "";
    [ObservableProperty] private string _regnumber = "";
    [ObservableProperty] private string _subject = "";
    [ObservableProperty] private string _bossname = "";
    [ObservableProperty] private string _bosstitle = "";
    [ObservableProperty] private string _exhitype = "";
    [ObservableProperty] private string _vystavki = "";
    [ObservableProperty] private string _exponat = "";

    public ObservableCollection<GrntiEntryVM> GrntiEntries { get; } = new();

    public long Id { get; private set; }
    public string Z2 { get; private set; } = "";
    public ExhibitTableRow? Result { get; private set; }
    public event EventHandler? CloseRequested;

    public ExhibitEditViewModel()
    {
        for (int i = 0; i < MaxGrnti; i++) GrntiEntries.Add(new GrntiEntryVM());
    }

    public ExhibitEditViewModel(ExhibitTableRow row) : this()
    {
        _title = "Изменение выставки";
        Id = row.Id;
        Z2 = row.Z2 ?? "";
        _codvuz = row.Codvuz ?? "";
        _type = row.Type ?? "";
        _regnumber = row.Regnumber ?? "";
        _subject = row.Subject ?? "";
        _bossname = row.Bossname ?? "";
        _bosstitle = row.Bosstitle ?? "";
        _exhitype = row.Exhitype ?? "";
        _vystavki = row.Vystavki ?? "";
        _exponat = row.Exponat ?? "";

        var codes = (row.Grnti ?? "")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Take(MaxGrnti)
            .ToList();

        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            if (code.Length < 2) continue;

            GrntiEntries[i].Rubric = code.Substring(0, 2);
            GrntiEntries[i].Sub = code.Length > 3 ? code.Substring(3) : "";
        }
    }

    [RelayCommand]
    private void Save()
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(Codvuz))
            issues.Add("Не выбран код ВУЗа");

        if (string.IsNullOrWhiteSpace(Type))
            issues.Add("Не выбран признак формы");

        if (string.IsNullOrWhiteSpace(Regnumber))
            issues.Add("Не заполнен регистрационный номер");

        if (string.IsNullOrWhiteSpace(Subject))
            issues.Add("Не заполнено наименование проекта");

        if (string.IsNullOrWhiteSpace(Bossname))
            issues.Add("Не заполнен руководитель");

        if (string.IsNullOrWhiteSpace(Bosstitle))
            issues.Add("Не заполнена должность/степень руководителя");

        if (string.IsNullOrWhiteSpace(Exhitype))
            issues.Add("Не выбрано наличие экспоната");

        var codes = new List<string>();
        foreach (var entry in GrntiEntries)
        {
            var rub = (entry.Rubric ?? "").Trim();
            var sub = (entry.Sub ?? "")
                .Replace("_", "")
                .Trim()
                .TrimEnd('.')
                .Trim();

            if (rub.Length == 0 && sub.Length == 0) continue;

            if (!Regex.IsMatch(rub, @"^\d{2}$"))
            {
                issues.Add($"Рубрика ГРНТИ должна быть из двух цифр: '{rub}'");
                continue;
            }

            if (sub.Length > 0 && !Regex.IsMatch(sub, @"^\d{2}(\.\d{2})?$"))
            {
                issues.Add($"Подрубрика ГРНТИ должна быть XX или XX.XX: '{sub}'");
                continue;
            }

            codes.Add(sub.Length == 0 ? rub : $"{rub}.{sub}");
        }

        if (codes.Count == 0)
            issues.Add("Не заполнено ни одного кода ГРНТИ");

        if (issues.Count > 0)
        {
            Error = string.Join("; ", issues);
            return;
        }

        var row = new ExhibitTableRow
        {
            Id = Id,
            Codvuz = Codvuz.Trim(),
            Z2 = Z2,
            Type = Type.Trim(),
            Regnumber = Regnumber.Trim(),
            Subject = Subject.Trim(),
            Grnti = GrntiService.ParseGrnti(string.Join(", ", codes)),
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