using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SQLWerk.Views;

// Leave most of the available height to the table; collapsed details use only their natural height.
public sealed class GroupDetailsHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double height && double.IsFinite(height) ? Math.Max(0, (height - 184) * 0.4) : 160d;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
