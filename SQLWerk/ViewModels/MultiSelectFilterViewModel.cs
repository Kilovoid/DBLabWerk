using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace SQLWerk.ViewModels;

public partial class FilterValueViewModel : ObservableObject
{
    public string Value { get; }
    public string Label { get; }
    [ObservableProperty] private bool _isSelected;
    public FilterValueViewModel(string value, bool selected, string? label = null)
    { Value = value; Label = label ?? (value.Length == 0 ? "(Пустые)" : value); _isSelected = selected; }
}

public partial class MultiSelectFilterViewModel : ObservableObject
{
    private readonly List<FilterValueViewModel> _values = new();
    private HashSet<string>? _selection;
    private bool _updating;
    public string Title { get; }
    public bool IsActive => _selection != null;
    public ObservableCollection<FilterValueViewModel> VisibleValues { get; } = new();
    public event EventHandler? SelectionChanged;
    [ObservableProperty] private string _searchText = "";
    public MultiSelectFilterViewModel(string title) => Title = title;
    public HashSet<string>? GetSelection() => _selection == null ? null : new(_selection, StringComparer.OrdinalIgnoreCase);

    public bool? AllVisibleSelected
    {
        get => VisibleValues.Count == 0 ? false : VisibleValues.All(v => v.IsSelected) ? true
            : VisibleValues.All(v => !v.IsSelected) ? false : null;
        set
        {
            if (value == null || _updating) return;
            _updating = true;
            try { foreach (var item in VisibleValues) item.IsSelected = value.Value; }
            finally { _updating = false; }
            OnPropertyChanged();
        }
    }
    public void SetValues(IEnumerable<string?> values, Func<string, string>? label = null)
    {
        foreach (var item in _values) item.PropertyChanged -= OnItemChanged;
        _values.Clear();
        foreach (var value in values.Select(v => (v ?? "").Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v))
        {
            var item = new FilterValueViewModel(value, _selection == null || _selection.Contains(value), label?.Invoke(value));
            item.PropertyChanged += OnItemChanged; _values.Add(item);
        }
        // Keep an explicit selection if all selected values disappear on reload.
        UpdateVisible();
    }
    public void BeginEdit()
    {
        SearchText = "";
        foreach (var item in _values) item.IsSelected = _selection == null || _selection.Contains(item.Value);
        UpdateVisible();
    }
    public void Apply()
    {
        var selected = VisibleValues.Where(v => v.IsSelected).Select(v => v.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _selection = _values.All(v => selected.Contains(v.Value)) ? null : selected;
        NotifyApplied();
    }
    public void RestoreSelection(HashSet<string>? selection)
    { _selection = selection == null ? null : new(selection, StringComparer.OrdinalIgnoreCase); BeginEdit(); NotifyApplied(); }
    public void Reset() => RestoreSelection(null);
    private void NotifyApplied()
    { OnPropertyChanged(nameof(IsActive)); SelectionChanged?.Invoke(this, EventArgs.Empty); }
    partial void OnSearchTextChanged(string value)
    {
        UpdateVisible();
        if (!string.IsNullOrWhiteSpace(value)) AllVisibleSelected = true;
    }
    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    { if (!_updating && e.PropertyName == nameof(FilterValueViewModel.IsSelected)) OnPropertyChanged(nameof(AllVisibleSelected)); }
    private void UpdateVisible()
    {
        var query = SearchText.Trim(); VisibleValues.Clear();
        foreach (var item in _values.Where(v => v.Label.Contains(query, StringComparison.OrdinalIgnoreCase))) VisibleValues.Add(item);
        OnPropertyChanged(nameof(AllVisibleSelected));
    }
}
