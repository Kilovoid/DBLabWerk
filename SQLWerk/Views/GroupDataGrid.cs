using Avalonia;
using Avalonia.Controls;
using System;

namespace SQLWerk.Views;

// The grid's automatic height can omit the horizontal scrollbar's space.
// Reserve a small bottom area while keeping MaxHeight and internal scrolling.
public sealed class GroupDataGrid : DataGrid
{
    protected override Type StyleKeyOverride => typeof(DataGrid);

    protected override Size MeasureOverride(Size availableSize)
    {
        var measured = base.MeasureOverride(availableSize);
        return new Size(measured.Width, Math.Min(availableSize.Height, measured.Height + 20));
    }
}
