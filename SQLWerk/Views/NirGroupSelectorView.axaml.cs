using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
    private Popup? _popup;
    private MainWindowViewModel? _vm;
    private bool _updating;
    private bool _opening;
    private bool _committing;
    private bool _creating;
    public bool IsDropDownOpen => _popup?.IsOpen == true;

    public NirGroupSelectorView()
    {
        InitializeComponent();
        _choices = this.FindControl<ListBox>("GroupChoices");
        _search = this.FindControl<TextBox>("GroupSearch");
        _popup = this.FindControl<Popup>("GroupPopup");
        if (_popup != null) _popup.PlacementTarget = this;
        _choices?.AddHandler(InputElement.PointerWheelChangedEvent, (_, e) => e.Handled = true,
            RoutingStrategies.Bubble, handledEventsToo: true);
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
        if (_committing) return;
        if (e.PropertyName == nameof(MainWindowViewModel.NirGroups)
            || e.PropertyName == nameof(MainWindowViewModel.SelectedNirGroup)) RefreshChoices();
    }

    public void FocusSelector() => Focus();

    private void OpenChoices()
    {
        if (_vm == null || _popup == null || _opening || _committing || _creating) return;
        _opening = true;
        try
        {
            RefreshChoices();
            if (this.FindControl<Border>("ChoicesPanel") is { } panel)
            {
                var windowWidth = (TopLevel.GetTopLevel(this) as Window)?.ClientSize.Width ?? 680;
                panel.Width = Math.Max(240, Math.Min(Math.Max(640, Bounds.Width), windowWidth - 32));
            }
            if (!_popup.IsOpen) _popup.IsOpen = true;
        }
        finally { _opening = false; }
    }

    // Open only for explicit input. Popup focus events must never rebuild its list.
    private void OnSearchPressed(object? sender, PointerPressedEventArgs e) => OpenChoices();
    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        if (_popup?.IsOpen == true) _popup.IsOpen = false;
        else { _search?.Focus(); OpenChoices(); }
    }
    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_updating && !_committing && _search?.IsFocused == true) OpenChoices();
    }
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            _popup!.IsOpen = false;
            Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Down && ReferenceEquals(e.Source, _search))
        {
            OpenChoices();
            _choices?.Focus();
            e.Handled = true;
        }
    }

    private void RefreshChoices()
    {
        if (_choices == null || _vm == null || _updating) return;
        _updating = true;
        try
        {
            var query = (_search?.Text ?? "").Trim();
            var groups = _vm.NirGroups.Where(g => query.Length == 0
                || g.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                || g.Comment.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            _choices.ItemsSource = groups;
            _choices.SelectedItem = groups.FirstOrDefault(g => g.Code == _vm.SelectedNirGroup?.Code);
        }
        finally { _updating = false; }
    }

    private void OnGroupChosen(object? sender, SelectionChangedEventArgs e)
    {
        if (_updating || _committing || _vm == null || !ReferenceEquals(e.Source, _choices)) return;
        var group = e.AddedItems.OfType<NirGroup>().FirstOrDefault();
        if (group == null) return;
        var vm = _vm;
        _committing = true;
        // Finish ListBox's selection event before rebuilding the group page or changing focus.
        if (_popup != null) _popup.IsOpen = false;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (!ReferenceEquals(_vm, vm)) return;
                vm.NirGroupSearch = "";
                vm.NirGroupCommentSearch = "";
                vm.SelectedNirGroup = group;
                _updating = true;
                try { if (_search != null) _search.Text = ""; }
                finally { _updating = false; }
                RefreshChoices();
                Focus();
            }
            finally { _committing = false; }
        });
    }

    private async void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (_creating || _vm == null || TopLevel.GetTopLevel(this) is not Window owner) return;
        _creating = true;
        try
        {
            if (_popup != null) _popup.IsOpen = false;
            Focus();
            var code = await new NewNirGroupWindow(_vm).ShowDialog<string?>(owner);
            if (code != null)
            {
                _updating = true;
                try { if (_search != null) _search.Text = ""; }
                finally { _updating = false; }
                RefreshChoices();
            }
        }
        finally { _creating = false; }
    }
}
