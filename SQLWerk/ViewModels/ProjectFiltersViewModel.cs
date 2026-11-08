using CommunityToolkit.Mvvm.ComponentModel;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.ViewModels;

// Each field uses the same small Excel-style value list. No extra UI dependency.
public class ProjectFiltersViewModel : ObservableObject
{
    private sealed record Field(MultiSelectFilterViewModel Filter, Func<FullModel, IEnumerable<string>> Values);
    private readonly List<Field> _fields = new();
    public IReadOnlyList<MultiSelectFilterViewModel> MultiFilters => _fields.Select(f => f.Filter).ToArray();
    public MultiSelectFilterViewModel this[string title] => _fields.First(f => f.Filter.Title == title).Filter;
    public ProjectFiltersViewModel()
    {
        void Add(string title, Func<FullModel, string?> value) => _fields.Add(new(new(title), r => new[] { (value(r) ?? "").Trim() }));
        Add("Рег. номер", r => r.Regnumber);
        Add("Проект", r => r.Subject);
        Add("Код вуза", r => r.Codvuz.ToString());
        Add("Вуз", r => r.Z1);
        Add("Полное название вуза", r => r.Z1Full);
        Add("Краткое название вуза", r => r.Z2);
        _fields.Add(new(new("Рубрика"), r => Codes(r.Grnti).Select(c => c.Substring(0, 2)).DefaultIfEmpty("")));
        _fields.Add(new(new("Подрубрика"), r => Codes(r.Grnti).SelectMany(c => c.Length > 2
            ? new[] { c.Length >= 5 ? c.Substring(0, 5) : c, c } : new[] { "" }).DefaultIfEmpty("")));
        _fields.Add(new(new("ГРНТИ"), r => Codes(r.Grnti).DefaultIfEmpty("")));
        Add("Название рубрики", r => r.Rubrika);
        Add("Форма НИР", r => r.Type);
        Add("Руководитель", r => r.Bossname);
        Add("Должность", r => r.Bosstitle);
        Add("Наличие экспоната", r => r.Exhitype);
        Add("Выставки", r => r.Vystavki);
        Add("Экспонат", r => r.Exponat);
        Add("Федеральный округ", r => r.Region);
        Add("Регион", r => r.OblName);
        Add("Город", r => r.City);
        Add("Код региона", r => r.Obl);
        Add("Тип учреждения", r => r.Status);
        Add("Статус", r => r.GrVed);
        Add("Специализация", r => r.Prof);
    }
    public static IEnumerable<string> Codes(string? raw) => (raw ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim()).Where(c => c.Length >= 2);
    public void SetData(IEnumerable<FullModel> rows, IEnumerable<VuzTableRow> vuzes)
    {
        var data = rows.ToList();
        var names = vuzes.GroupBy(v => v.Codvuz.ToString()).ToDictionary(g => g.Key,
            g => (string.IsNullOrWhiteSpace(g.First().Z1Full) ? g.First().Z1 : g.First().Z1Full) ?? g.First().Z2 ?? "");
        foreach (var field in _fields)
            field.Filter.SetValues(data.SelectMany(field.Values), field.Filter.Title == "Код вуза"
                ? code => names.TryGetValue(code, out var name) ? $"{code} — {name}" : code : null);
    }
    public Func<FullModel, bool> CreatePredicate()
    {
        var conditions = _fields.Select(f => (Field: f, Selection: f.Filter.GetSelection())).Where(c => c.Selection != null).ToArray();
        var rubrics = this["Рубрика"].GetSelection(); var subs = this["Подрубрика"].GetSelection();
        return row => conditions.All(c => c.Field.Values(row).Any(c.Selection!.Contains))
            && (rubrics == null || subs == null || Codes(row.Grnti).Any(code => rubrics.Contains(code.Substring(0, 2))
                && subs.Any(sub => sub.Length == 0 ? code.Length == 2 : code == sub || code.StartsWith(sub + ".", StringComparison.OrdinalIgnoreCase))));
    }
    public void Reset() { foreach (var field in _fields) field.Filter.Reset(); }
}
