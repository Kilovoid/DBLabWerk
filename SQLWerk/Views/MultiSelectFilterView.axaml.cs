using Avalonia.Controls;
using System;

namespace SQLWerk.Views;

public partial class MultiSelectFilterView : UserControl
{
    public MultiSelectFilterView() => InitializeComponent();

    private void OnFilterOpening(object? sender, EventArgs e)
    {
        // Передаем текущую модель фильтра в отдельную всплывающую панель.
        if (sender is Flyout { Content: Control panel })
            panel.DataContext = DataContext;
    }
}
