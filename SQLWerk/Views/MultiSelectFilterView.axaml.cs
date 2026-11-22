using Avalonia.Controls;
using Avalonia.Interactivity;
using SQLWerk.ViewModels;
using System;
namespace SQLWerk.Views;
public partial class MultiSelectFilterView : UserControl
{
    private Flyout? _flyout;
    private MultiSelectFilterViewModel? _editing;
    private ListBox? _valuesList;
    public MultiSelectFilterView() => InitializeComponent();
    private void OnFilterOpening(object? sender, EventArgs e)
    {
        if (sender is not Flyout flyout || DataContext is not MultiSelectFilterViewModel vm) return;
        _flyout = flyout;
        _editing = vm;
        if (flyout.Content is Control panel) panel.DataContext = vm;
        vm.BeginEdit();
        if (_valuesList != null && vm.VisibleValues.Count > 0)
            _valuesList.ScrollIntoView(vm.VisibleValues[0]);
    }
    private void OnValuesListLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not ListBox list) return;
        if (!ReferenceEquals(_valuesList, list))
        {
            _valuesList?.RemoveHandler(ScrollViewer.ScrollChangedEvent, OnValuesScrolled);
            _valuesList = list;
            list.AddHandler(ScrollViewer.ScrollChangedEvent, OnValuesScrolled);
        }
        if (_editing?.VisibleValues.Count > 0) list.ScrollIntoView(_editing.VisibleValues[0]);
    }
    private void OnValuesScrolled(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source is ScrollViewer viewer && viewer.Offset.Y > 0
            && viewer.Extent.Height - viewer.Viewport.Height - viewer.Offset.Y < 160)
            _editing?.LoadMore();
    }
    private void OnFilterClosed(object? sender, EventArgs e)
    {
        _editing?.EndEdit();
        _editing = null;
    }
    private void OnApply(object? sender, RoutedEventArgs e)
    { _editing?.Apply(); _flyout?.Hide(); }
    private void OnCancel(object? sender, RoutedEventArgs e) => _flyout?.Hide();
}
