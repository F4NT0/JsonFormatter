using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace JsonFormatter.Converters;

public class BoolToCheckConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? "✓" : "✗";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
