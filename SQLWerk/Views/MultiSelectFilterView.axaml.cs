using Avalonia.Controls;
using Avalonia.Interactivity;
using SQLWerk.ViewModels;
using System;
namespace SQLWerk.Views;
public partial class MultiSelectFilterView : UserControl
{
    private Flyout? _flyout;
    public MultiSelectFilterView() => InitializeComponent();
    private void OnFilterOpening(object? sender, EventArgs e)
    {
        if (sender is not Flyout flyout || DataContext is not MultiSelectFilterViewModel vm) return;
        _flyout = flyout;
        vm.BeginEdit();
        if (flyout.Content is Control panel) panel.DataContext = vm;
    }
    private void OnApply(object? sender, RoutedEventArgs e)
    { if (DataContext is MultiSelectFilterViewModel vm) vm.Apply(); _flyout?.Hide(); }
    private void OnCancel(object? sender, RoutedEventArgs e) => _flyout?.Hide();
}
