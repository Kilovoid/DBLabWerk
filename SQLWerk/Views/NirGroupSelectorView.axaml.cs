using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using SQLWerk.Models;
using SQLWerk.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;

namespace SQLWerk.Views;
public partial class NirGroupSelectorView : UserControl
{
    private ListBox? _choices;
    private TextBox? _search;
    private MainWindowViewModel? _vm;
    private Popup? _popup;
    public bool IsDropDownOpen => _popup?.IsOpen == true;
    private bool _updating;
    public NirGroupSelectorView()
    {
        InitializeComponent();
        _choices = this.FindControl<ListBox>("GroupChoices");
        _search = this.FindControl<TextBox>("GroupSearch");
        _popup = this.FindControl<Popup>("GroupPopup");
        if (_popup != null) _popup.PlacementTarget = _search;
        _choices?.AddHandler(InputElement.PointerWheelChangedEvent, (_, e) => e.Handled = true,
            RoutingStrategies.Bubble, handledEventsToo: true);
        RefreshChoices();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= OnGroupsChanged;
        base.OnDataContextChanged(e);
        _vm = DataContext as MainWindowViewModel;
        if (_vm != null) _vm.PropertyChanged += OnGroupsChanged;
        RefreshChoices();
    }
    private void OnGroupsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.NirGroups)) RefreshChoices();
        else if (e.PropertyName == nameof(MainWindowViewModel.SelectedNirGroup))
        {
            if (_vm?.SelectedNirGroup is { } selected && _choices?.ItemsSource is NirGroup[] groups
                && !groups.Any(g => g.Code == selected.Code) && _search != null)
            {
                _updating = true;
                try { _search.Text = ""; RefreshChoices(); }
                finally { _updating = false; }
            }
            SyncSelection();
        }
    }
    public void FocusSelector() => Focus();
    private void OpenChoices()
    {
        if (_vm == null || _popup == null) return;
        RefreshChoices();
        if (this.FindControl<Border>("ChoicesPanel") is { } panel)
        {
            var windowWidth = (TopLevel.GetTopLevel(this) as Window)?.ClientSize.Width ?? 680;
            panel.Width = Math.Max(240, Math.Min(Math.Max(640, Bounds.Width), windowWidth - 32));
        }
        _popup.IsOpen = true;
    }
    private void OnSearchFocused(object? sender, RoutedEventArgs e)
    { if (!_updating) OpenChoices(); }
    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        if (_popup?.IsOpen == true) _popup.IsOpen = false;
        else { OpenChoices(); _search?.Focus(); }
    }
    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    { if (!_updating) OpenChoices(); }
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _popup?.IsOpen != true) return;
        _popup.IsOpen = false;
        Focus();
        e.Handled = true;
    }
    private void RefreshChoices()
    {
        if (_choices == null || _vm == null) return;
        var query = (_search?.Text ?? "").Trim();
        var groups = _vm.NirGroups.Where(g => query.Length == 0
            || g.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
            || g.Comment.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        _updating = true;
        try
        {
            _choices.ItemsSource = groups;
            _choices.SelectedItem = groups.FirstOrDefault(g => g.Code == _vm.SelectedNirGroup?.Code);
        }
        finally { _updating = false; }
    }
    private void SyncSelection()
    {
        if (_choices?.ItemsSource is not NirGroup[] groups || _vm == null) return;
        var previous = _updating; _updating = true;
        try { _choices.SelectedItem = groups.FirstOrDefault(g => g.Code == _vm.SelectedNirGroup?.Code); }
        finally { _updating = previous; }
    }
    private void OnGroupChosen(object? sender, SelectionChangedEventArgs e)
    {
        if (_updating || _vm == null) return;
        var group = e.AddedItems.OfType<NirGroup>().FirstOrDefault();
        if (group == null) return;
        _vm.NirGroupSearch = "";
        _vm.NirGroupCommentSearch = "";
        _vm.SelectedNirGroup = group;
        _updating = true;
        try { if (_search != null) _search.Text = ""; }
        finally { _updating = false; }
        if (_popup != null) _popup.IsOpen = false;
        Focus();
    }
    private async void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || TopLevel.GetTopLevel(this) is not Window owner) return;
        if (_popup != null) _popup.IsOpen = false;
        Focus();
        var code = await new NewNirGroupWindow(_vm).ShowDialog<string?>(owner);
        if (code != null && _search != null)
        {
            _updating = true;
            try { _search.Text = ""; RefreshChoices(); }
            finally { _updating = false; }
        }
    }
}
