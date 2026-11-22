using Avalonia.Threading;
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
    private sealed record Option(string Value, string Label);
    private const int PageSize = 80;
    private List<Option> _values = new();
    private IReadOnlyList<Option> _matches = Array.Empty<Option>();
    private HashSet<string>? _selection;
    private HashSet<string>? _draft;
    private bool _updating;
    private bool _editing;
    private int _selectedMatchCount;
    private string _currentQuery = "";
    private readonly DispatcherTimer _searchTimer;
    public string Title { get; }
    public bool IsActive => _selection != null;
    public ObservableCollection<FilterValueViewModel> VisibleValues { get; } = new();
    public event EventHandler? SelectionChanged;
    [ObservableProperty] private string _searchText = "";

    public MultiSelectFilterViewModel(string title)
    {
        Title = title;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _searchTimer.Tick += (_, _) => FlushSearch();
    }

    public HashSet<string>? GetSelection() => _selection == null ? null : new(_selection, StringComparer.OrdinalIgnoreCase);
    private bool IsSelected(string value) => _draft == null || _draft.Contains(value);

    public bool? AllVisibleSelected
    {
        get => _matches.Count == 0 ? false : _selectedMatchCount == _matches.Count ? true
            : _selectedMatchCount == 0 ? false : null;
        set
        {
            if (value == null || _updating || !_editing) return;
            FlushSearch();
            SetMatchesSelected(value.Value);
        }
    }

    public void SetValues(IEnumerable<string?> values, Func<string, string>? label = null)
    {
        _values = values.Select(v => (v ?? "").Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v).Select(v => new Option(v, label?.Invoke(v)
                ?? (v.Length == 0 ? "(Пустые)" : v))).ToList();
        // Applied selection remains valid even if the imported values have changed.
        if (_editing) BeginEdit();
        else ClearVisible();
    }

    public void BeginEdit()
    {
        _searchTimer.Stop();
        _editing = true;
        _draft = _selection == null ? null : new(_selection, StringComparer.OrdinalIgnoreCase);
        _updating = true;
        try { SearchText = ""; }
        finally { _updating = false; }
        UpdateMatches(false);
    }

    public void EndEdit()
    {
        _searchTimer.Stop();
        _editing = false;
        _draft = null;
        _matches = Array.Empty<Option>();
        _selectedMatchCount = 0;
        ClearVisible();
    }

    public void LoadMore()
    {
        if (!_editing || _updating || _searchTimer.IsEnabled) return;
        _updating = true;
        try
        {
            var start = VisibleValues.Count;
            var end = Math.Min(start + PageSize, _matches.Count);
            for (var i = start; i < end; i++)
            {
                var option = _matches[i];
                var item = new FilterValueViewModel(option.Value, IsSelected(option.Value), option.Label);
                item.PropertyChanged += OnItemChanged;
                VisibleValues.Add(item);
            }
        }
        finally { _updating = false; }
    }

    public void Apply()
    {
        if (!_editing) return;
        FlushSearch();
        // Apply the whole result, including matches not loaded into the UI yet.
        if (_currentQuery.Length == 0 && _draft == null) _selection = null;
        else
        {
            var selected = _matches.Where(option => IsSelected(option.Value)).Select(option => option.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _selection = _values.All(option => selected.Contains(option.Value)) ? null : selected;
        }
        NotifyApplied();
    }

    public void RestoreSelection(HashSet<string>? selection)
    {
        _selection = selection == null ? null : new(selection, StringComparer.OrdinalIgnoreCase);
        if (_editing) BeginEdit();
        NotifyApplied();
    }
    public void Reset() => RestoreSelection(null);
    private void NotifyApplied()
    { OnPropertyChanged(nameof(IsActive)); SelectionChanged?.Invoke(this, EventArgs.Empty); }

    partial void OnSearchTextChanged(string value)
    {
        if (_updating || !_editing) return;
        _searchTimer.Stop();
        _searchTimer.Start();
    }
    private void FlushSearch()
    {
        _searchTimer.Stop();
        if (_editing && !string.Equals(_currentQuery, SearchText.Trim(), StringComparison.Ordinal))
            UpdateMatches(!string.IsNullOrWhiteSpace(SearchText));
    }
    private void UpdateMatches(bool selectMatches)
    {
        _currentQuery = SearchText.Trim();
        _matches = _currentQuery.Length == 0 ? _values
            : _values.Where(option => option.Label.Contains(_currentQuery, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (selectMatches && _draft != null)
            foreach (var option in _matches) _draft.Add(option.Value);
        _selectedMatchCount = _draft == null ? _matches.Count : _matches.Count(option => _draft.Contains(option.Value));
        ClearVisible();
        LoadMore();
        OnPropertyChanged(nameof(AllVisibleSelected));
    }
    private void SetMatchesSelected(bool selected)
    {
        if (_matches.Count == _values.Count)
            _draft = selected ? null : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        else
        {
            if (!selected && _draft == null)
                _draft = _values.Select(option => option.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (_draft != null)
                foreach (var option in _matches)
                    if (selected) _draft.Add(option.Value); else _draft.Remove(option.Value);
        }
        _selectedMatchCount = selected ? _matches.Count : 0;
        _updating = true;
        try { foreach (var item in VisibleValues) item.IsSelected = selected; }
        finally { _updating = false; }
        OnPropertyChanged(nameof(AllVisibleSelected));
    }
    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_updating || e.PropertyName != nameof(FilterValueViewModel.IsSelected)
            || sender is not FilterValueViewModel item) return;
        if (_draft == null)
        {
            if (item.IsSelected) return;
            _draft = _values.Select(option => option.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        if (item.IsSelected)
        { if (_draft.Add(item.Value)) _selectedMatchCount++; }
        else if (_draft.Remove(item.Value)) _selectedMatchCount--;
        OnPropertyChanged(nameof(AllVisibleSelected));
    }
    private void ClearVisible()
    {
        foreach (var item in VisibleValues) item.PropertyChanged -= OnItemChanged;
        VisibleValues.Clear();
    }
}
