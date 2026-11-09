using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using SQLWerk.Models;
using SQLWerk.ViewModels;

namespace SQLWerk.Views;

public partial class MainWindow : Window
{
    private LogWindow? _logWindow;
    private MainWindowViewModel? _boundVm;
    private Action? _pendingNavigation;
    private bool _groupTableScrollActive;
    private bool _activationClickPending;
    private readonly HashSet<Window> _navigationDialogs = new();

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerWheelChangedEvent, OnGroupPageWheel, RoutingStrategies.Tunnel);
        AddHandler(InputElement.PointerPressedEvent, OnPagePointerPressed, RoutingStrategies.Tunnel);
        AddHandler(InputElement.PointerReleasedEvent, OnPagePointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(InputElement.KeyDownEvent, OnGroupTableEscape, RoutingStrategies.Tunnel);
        // Once activated, the table consumes its own wheel events, including at its edges.
        this.FindControl<DataGrid>("NirGroupsGrid")?.AddHandler(
            InputElement.PointerWheelChangedEvent, (_, e) => e.Handled = true,
            RoutingStrategies.Bubble, handledEventsToo: true);
        this.FindControl<TextBox>("GroupCommentEditor")?.AddHandler(
            InputElement.PointerWheelChangedEvent, (_, e) => e.Handled = true,
            RoutingStrategies.Bubble, handledEventsToo: true);
        // The grid scrolls its rows; row focus must not scroll the whole group page.
        this.FindControl<DataGrid>("NirGroupsGrid")?.AddHandler(
            Control.RequestBringIntoViewEvent, (_, e) => e.Handled = true,
            RoutingStrategies.Bubble);
    }

    private bool IsInGroupTable(object? source)
    {
        var grid = this.FindControl<DataGrid>("NirGroupsGrid");
        return source is Control control && (ReferenceEquals(control, grid) || control.FindAncestorOfType<DataGrid>() == grid);
    }

    private void SetGroupTableActive(bool active)
    {
        _groupTableScrollActive = active;
        var grid = this.FindControl<DataGrid>("NirGroupsGrid");
        if (grid == null) return;
        if (active) grid.Classes.Add("scroll-active");
        else grid.Classes.Remove("scroll-active");
    }

    private void OnPagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _activationClickPending = false;
        if (!IsInGroupTable(e.Source)) SetGroupTableActive(false);
        else OnGroupTablePressed(this.FindControl<DataGrid>("NirGroupsGrid"), e);
    }

    private void OnGroupPageWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_groupTableScrollActive || !IsInGroupTable(e.Source)) return;
        if (this.FindControl<ScrollViewer>("GroupDetailsScroll") is not { } page) return;
        var max = Math.Max(0, page.Extent.Height - page.Viewport.Height);
        page.Offset = new Avalonia.Vector(page.Offset.X, Math.Clamp(page.Offset.Y - e.Delta.Y * 48, 0, max));
        // Do not allow the inactive table to consume the wheel: it scrolls the page instead.
        e.Handled = true;
    }

    private void OnGroupTablePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (!_groupTableScrollActive)
        {
            SetGroupTableActive(true);
            _activationClickPending = true;
            grid.SelectedItems.Clear();
            grid.Focus();
            grid.SelectedItems.Clear();
            e.Handled = true;
            return;
        }
        if (!e.GetCurrentPoint(grid).Properties.IsRightButtonPressed) return;
        var control = e.Source as Control;
        var row = control as DataGridRow ?? control?.FindAncestorOfType<DataGridRow>();
        if (row?.DataContext is not NirGroupEntry entry || grid.SelectedItems.Contains(entry)) return;
        grid.SelectedItems.Clear();
        grid.SelectedItem = entry;
    }

    private void OnPagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_activationClickPending) return;
        _activationClickPending = false;
        this.FindControl<DataGrid>("NirGroupsGrid")?.SelectedItems.Clear();
        e.Handled = true;
    }

    private void OnGroupTableEscape(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || DataContext is not MainWindowViewModel { ShowNirGroups: true }) return;
        if (this.FindControl<NirGroupSelectorView>("GroupSelector")?.IsDropDownOpen == true) return;
        var grid = this.FindControl<DataGrid>("NirGroupsGrid");
        if (!_groupTableScrollActive && (grid == null || grid.SelectedItems.Count == 0)) return;
        grid?.SelectedItems.Clear();
        _activationClickPending = false;
        SetGroupTableActive(false);
        e.Handled = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_boundVm != null)
        {
            _boundVm.ShowLogRequested -= OnShowLog;
            _boundVm.NavigateToRecordRequested -= OnNavigateToRecord;
            _boundVm.NavigateToGroupRequested -= OnNavigateToGroup;
        }
        _pendingNavigation = null;
        _boundVm = null;
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            _boundVm = vm;
            vm.ShowLogRequested += OnShowLog;
            vm.NavigateToRecordRequested += OnNavigateToRecord;
            vm.NavigateToGroupRequested += OnNavigateToGroup;
            vm.SelectNirGroupsHandler = ShowNirGroupPickerAsync;
            vm.GrntiEditHandler = ShowGrntiEditAsync;
            vm.ConfirmHandler = ShowConfirmAsync;
            vm.VuzEditHandler = ShowVuzEditAsync;
            vm.ExhibitEditHandler = ShowExhibitEditAsync;
        }
    }

    private void QueueNavigation(Action navigate)
    {
        _pendingNavigation = navigate;
        TryRunNavigation();
    }

    private void TryRunNavigation()
    {
        if (_pendingNavigation == null) return;
        // Let creation/selection dialogs finish before focusing the main UI.
        var dialogs = OwnedWindows.Where(w => w.IsVisible && w is not LogWindow).ToArray();
        if (dialogs.Length != 0)
        {
            foreach (var dialog in dialogs)
                if (_navigationDialogs.Add(dialog)) dialog.Closed += OnNavigationDialogClosed;
            return;
        }
        var navigate = _pendingNavigation;
        _pendingNavigation = null;
        Dispatcher.UIThread.Post(navigate, DispatcherPriority.Background);
    }

    private void OnNavigationDialogClosed(object? sender, EventArgs e)
    {
        if (sender is Window dialog)
        {
            dialog.Closed -= OnNavigationDialogClosed;
            _navigationDialogs.Remove(dialog);
        }
        Dispatcher.UIThread.Post(TryRunNavigation, DispatcherPriority.Background);
    }

    private void OnNavigateToRecord(string table, long id)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        QueueNavigation(() =>
        {
            if (!ReferenceEquals(DataContext, vm)) return;
            var status = vm.Status;
            DataGrid? grid = null;
            object? item = null;
            switch (table)
            {
                case "Exhibit":
                    vm.SelectExhibits();
                    grid = this.FindControl<DataGrid>("ExhibitsGrid");
                    item = vm.Exhibits.FirstOrDefault(r => r.Id == id);
                    break;
                case "Vuz":
                    vm.SelectVuz();
                    grid = this.FindControl<DataGrid>("VuzGrid");
                    item = vm.Vuzes.FirstOrDefault(r => r.Id == id);
                    break;
                case "Grnti":
                    vm.SelectGrnti();
                    grid = this.FindControl<DataGrid>("GrntiGrid");
                    item = vm.Grnti.FirstOrDefault(r => r.Id == id);
                    break;
            }
            vm.Status = status;
            if (grid == null || item == null) return;
            // Realize the correct table before selecting a row in a sorted/virtualized grid.
            grid.UpdateLayout();
            grid.SelectedItems.Clear();
            grid.SelectedItem = item;
            grid.ScrollIntoView(item, grid.Columns.FirstOrDefault(c => c.IsVisible));
            Dispatcher.UIThread.Post(() =>
            {
                if (!ReferenceEquals(DataContext, vm) || !grid.IsEffectivelyVisible) return;
                grid.UpdateLayout();
                grid.SelectedItem = item;
                grid.Focus();
                grid.ScrollIntoView(item, grid.Columns.FirstOrDefault(c => c.IsVisible));
            }, DispatcherPriority.Loaded);
        });
    }

    private void OnNavigateToGroup(string code)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        QueueNavigation(() =>
        {
            if (!ReferenceEquals(DataContext, vm)) return;
            vm.NirGroupSearch = "";
            vm.NirGroupCommentSearch = "";
            vm.ShowNirGroupCommand.Execute(null);
            vm.SelectedNirGroup = vm.NirGroups.FirstOrDefault(g => g.Code == code);
            Dispatcher.UIThread.Post(() =>
            {
                if (!ReferenceEquals(DataContext, vm) || !vm.ShowNirGroups) return;
                this.FindControl<ScrollViewer>("GroupDetailsScroll")?.ScrollToHome();
                this.FindControl<NirGroupSelectorView>("GroupSelector")?.FocusSelector();
                this.FindControl<Control>("SelectedGroupHeader")?.BringIntoView();
            }, DispatcherPriority.Loaded);
        });
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not DataGrid grid) return;
        switch (grid.Name)
        {
            case "ExhibitsGrid":
                vm.SelectedExhibits = grid.SelectedItems.OfType<ExhibitTableRow>().ToList();
                break;
            case "VuzGrid":
                vm.SelectedVuzes = grid.SelectedItems.OfType<VuzTableRow>().ToList();
                break;
            case "NirGroupsGrid":
                vm.SelectedGroupRows = grid.SelectedItems.OfType<NirGroupEntry>().ToList();
                break;
            case "FullGrid":
                vm.SelectedFullRows = grid.SelectedItems.OfType<FullModel>().ToList();
                break;
            case "GrntiGrid":
                vm.SelectedGrntiRows = grid.SelectedItems.OfType<GrntiTableRow>().ToList();
                break;
        }
    }

    private async Task<string[]?> ShowNirGroupPickerAsync()
    {
        if (DataContext is not MainWindowViewModel vm) return null;
        return await new NirGroupPickerWindow(vm).ShowDialog<string[]?>(this);
    }

    private void OnShowLog(object? sender, EventArgs e)
    {
        if (_logWindow is { IsVisible: true })
        {
            _logWindow.Activate();
            return;
        }

        _logWindow = new LogWindow
        {
            DataContext = App.Services.GetRequiredService<LogViewModel>()
        };
        _logWindow.Closed += (_, _) => _logWindow = null;
        _logWindow.Show(this);
    }

    private async Task<GrntiTableRow?> ShowGrntiEditAsync(GrntiTableRow? existing)
    {
        var dialogVm = existing is null
            ? new GrntiEditViewModel()
            : new GrntiEditViewModel(existing);

        var dlg = new GrntiEditWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<GrntiTableRow?>(this);
    }

    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialogVm = new ConfirmViewModel(title, message);
        var dlg = new ConfirmWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<bool>(this);
    }

    private async Task<VuzTableRow?> ShowVuzEditAsync(VuzTableRow? existing)
    {
        var dialogVm = existing is null
            ? new VuzEditViewModel()
            : new VuzEditViewModel(existing);

        var dlg = new VuzEditWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<VuzTableRow?>(this);
    }

    private async Task<ExhibitTableRow?> ShowExhibitEditAsync(ExhibitTableRow? existing)
    {
        var dialogVm = existing is null
            ? new ExhibitEditViewModel()
            : new ExhibitEditViewModel(existing);

        var dlg = new ExhibitEditWindow { DataContext = dialogVm };
        dialogVm.CloseRequested += (_, _) => dlg.Close(dialogVm.Result);

        return await dlg.ShowDialog<ExhibitTableRow?>(this);
    }
}
