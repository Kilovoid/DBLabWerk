using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace SQLWerk.ViewModels;

public partial class FilterValueViewModel : ObservableObject
{
    public string Value { get; }
    public string Label => Value.Length == 0 ? "(Пусто)" : Value;

    [ObservableProperty] private bool _isSelected;

    public FilterValueViewModel(string value, bool isSelected)
    {
        Value = value;
        _isSelected = isSelected;
    }
}

public partial class MultiSelectFilterViewModel : ObservableObject
{
    private readonly List<FilterValueViewModel> _values = new();
    private bool _updating;

    public string Title { get; }
    public ObservableCollection<FilterValueViewModel> VisibleValues { get; } = new();
    public event EventHandler? SelectionChanged;

    [ObservableProperty] private string _searchText = "";

    public string Summary
    {
        get
        {
            var selected = _values.Where(value => value.IsSelected).ToList();
            if (selected.Count == _values.Count) return "Все значения";
            if (selected.Count == 0) return "Ничего не выбрано";
            return selected.Count <= 2
                ? string.Join(", ", selected.Select(value => value.Label))
                : $"Выбрано: {selected.Count} из {_values.Count}";
        }
    }

    public MultiSelectFilterViewModel(string title) => Title = title;

    // null означает отсутствие ограничения. Пустой набор — не выбрано ничего.
    public HashSet<string>? GetSelection() => _values.All(value => value.IsSelected)
        ? null
        : _values.Where(value => value.IsSelected)
            .Select(value => value.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public void SetValues(IEnumerable<string?> values)
    {
        var selection = GetSelection();
        foreach (var item in _values)
            item.PropertyChanged -= OnValueChanged;
        _values.Clear();

        foreach (var value in values.Select(value => (value ?? "").Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value))
        {
            var item = new FilterValueViewModel(value, selection == null || selection.Contains(value));
            item.PropertyChanged += OnValueChanged;
            _values.Add(item);
        }

        UpdateVisibleValues();
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnSearchTextChanged(string value) => UpdateVisibleValues();

    private void UpdateVisibleValues()
    {
        var search = (SearchText ?? "").Trim();
        VisibleValues.Clear();
        foreach (var item in _values.Where(value =>
                     value.Label.Contains(search, StringComparison.OrdinalIgnoreCase)))
            VisibleValues.Add(item);
    }

    private void OnValueChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_updating && e.PropertyName == nameof(FilterValueViewModel.IsSelected))
            NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(Summary));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // При поиске меняем только показанные варианты, скрытые галочки сохраняем.
    [RelayCommand]
    private void SelectVisible() => SetSelected(VisibleValues, true);

    [RelayCommand]
    private void ClearVisible() => SetSelected(VisibleValues, false);

    [RelayCommand]
    private void SelectOnlyVisible()
    {
        var visible = VisibleValues.ToHashSet();
        _updating = true;
        try
        {
            foreach (var value in _values)
                value.IsSelected = visible.Contains(value);
        }
        finally { _updating = false; }
        NotifySelectionChanged();
    }

    private void SetSelected(IEnumerable<FilterValueViewModel> values, bool selected)
    {
        _updating = true;
        try
        {
            foreach (var value in values)
                value.IsSelected = selected;
        }
        finally { _updating = false; }
        NotifySelectionChanged();
    }

    [RelayCommand]
    public void Reset()
    {
        SearchText = "";
        SetSelected(_values, true);
    }
}
