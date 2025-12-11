using System;
using Avalonia.Data.Converters;

namespace uchat_client.Presentation.Converters;

public class BooleanToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var param = parameter as string ?? string.Empty;
        var parts = param.Split(',');
        var trueText = parts.Length > 0 ? parts[0] : "True";
        var falseText = parts.Length > 1 ? parts[1] : "False";
        return value is bool b && b ? trueText : falseText;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

