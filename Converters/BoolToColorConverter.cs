using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace JsonFormatter.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var paramStr = parameter as string ?? "#FFFFFF|#888888";
        var parts = paramStr.Split('|');
        var trueColor = parts.Length > 0 ? parts[0] : "#FFFFFF";
        var falseColor = parts.Length > 1 ? parts[1] : "#888888";

        var colorStr = (value is bool b && b) ? trueColor : falseColor;

        if (targetType == typeof(IBrush) || targetType == typeof(Avalonia.Media.IBrush))
            return new SolidColorBrush(Color.Parse(colorStr));

        return colorStr;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
