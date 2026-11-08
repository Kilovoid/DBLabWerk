using Avalonia.Controls;
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
    private bool _updating;
    public NirGroupSelectorView()
    {
        InitializeComponent();
        _choices = this.FindControl<ListBox>("GroupChoices");
        _search = this.FindControl<TextBox>("GroupSearch");
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
                _search.Text = "";
            SyncSelection();
        }
    }
    public void FocusSelector() => _search?.Focus();
    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    { if (!_updating) RefreshChoices(); }
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
    }
    private async void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || TopLevel.GetTopLevel(this) is not Window owner) return;
        var code = await new NewNirGroupWindow(_vm).ShowDialog<string?>(owner);
        if (code != null && _search != null) _search.Text = "";
    }
}
