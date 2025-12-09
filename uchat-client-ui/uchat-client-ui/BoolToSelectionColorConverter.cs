// BoolToSelectionColorConverter.cs

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace uchat_client.Converters;

/// <summary>
/// Converts bool (IsSelected) to border brush color for contact selection
/// </summary>
public class BoolToSelectionColorConverter : IValueConverter
{
    public static readonly BoolToSelectionColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return new SolidColorBrush(Color.Parse("#7AA8E0")); // Light blue when selected
        }
        return new SolidColorBrush(Color.Parse("#5A7AA1")); // Default border color
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}