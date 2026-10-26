using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using SQLWerk.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.Views;

public partial class NirGroupPickerWindow : Window
{
    private readonly MainWindowViewModel? _vm;
    private readonly Dictionary<string, CheckBox> _choices = new(StringComparer.Ordinal);
    private readonly StackPanel _options;
    private readonly TextBlock _emptyHint;
    private readonly TextBlock _summary;
    private readonly Button _addButton;
    private readonly Button _newGroupButton;
    private readonly Button _groupsButton;
    private readonly TextBox _search;
    private readonly Popup _popup;

    public NirGroupPickerWindow()
    {
        InitializeComponent();
        _options = this.FindControl<StackPanel>("GroupOptions")!;
        _emptyHint = this.FindControl<TextBlock>("EmptyHint")!;
        _summary = this.FindControl<TextBlock>("SelectionSummary")!;
        _addButton = this.FindControl<Button>("AddButton")!;
        _newGroupButton = this.FindControl<Button>("NewGroupButton")!;
        _groupsButton = this.FindControl<Button>("GroupsButton")!;
        _search = this.FindControl<TextBox>("GroupSearch")!;
        _popup = this.FindControl<Popup>("GroupsPopup")!;
        _popup.PlacementTarget = _groupsButton;
        _popup.Opened += (_, _) => _search.Focus();
        Closed += (_, _) => _popup.IsOpen = false;
    }

    public NirGroupPickerWindow(MainWindowViewModel vm) : this()
    {
        _vm = vm;
        RefreshOptions(vm.SelectedNirGroup?.Code);
    }

    private void RefreshOptions(string? selectCode = null)
    {
        if (_vm is null) return;
        var selected = _choices.Where(item => item.Value.IsChecked == true)
            .Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        if (selectCode != null) selected.Add(selectCode);

        _options.Children.Clear();
        _choices.Clear();
        foreach (var group in _vm.NirGroups)
        {
            var checkBox = new CheckBox
            {
                Content = new TextBlock { Text = group.Code, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                IsChecked = selected.Contains(group.Code)
            };
            checkBox.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == nameof(CheckBox.IsChecked)) UpdateSelection();
            };
            _choices.Add(group.Code, checkBox);
            _options.Children.Add(checkBox);
        }
        _emptyHint.IsVisible = _choices.Count == 0;
        ApplySearch(_search.Text);
        UpdateSelection();
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox input) ApplySearch(input.Text);
    }

    private void ApplySearch(string? text)
    {
        var search = (text ?? "").Trim();
        foreach (var item in _choices)
            item.Value.IsVisible = item.Key.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private string[] SelectedCodes() => _choices.Where(item => item.Value.IsChecked == true)
        .Select(item => item.Key).ToArray();

    private void UpdateSelection()
    {
        var codes = SelectedCodes();
        _addButton.IsEnabled = codes.Length > 0;
        _summary.Text = $"Выбрано групп: {codes.Length}";
        _groupsButton.Content = codes.Length == 0 ? "Выберите группы ▾"
            : codes.Length <= 2 ? string.Join(", ", codes) + " ▾"
            : $"Выбрано групп: {codes.Length} ▾";
    }

    private async void OnNewGroupClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        _newGroupButton.IsEnabled = false;
        _popup.IsOpen = false;
        try
        {
            var code = await new NewNirGroupWindow(_vm).ShowDialog<string?>(this);
            if (code != null)
            {
                _search.Text = "";
                RefreshOptions(code);
            }
        }
        finally
        {
            _newGroupButton.IsEnabled = true;
            if (IsVisible) _popup.IsOpen = true;
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var codes = SelectedCodes();
        if (codes.Length == 0) return;
        _popup.IsOpen = false;
        Close(codes);
    }

    private void OnGroupsButtonClick(object? sender, RoutedEventArgs e) =>
        _popup.IsOpen = !_popup.IsOpen;

    private void OnPopupKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _popup.IsOpen)
        {
            _popup.IsOpen = false;
            e.Handled = true;
            _groupsButton.Focus();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _popup.IsOpen = false;
        Close();
    }
}
