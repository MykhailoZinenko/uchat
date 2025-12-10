using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace uchat_client.Converters;

/// <summary>
/// Converts bool to HorizontalAlignment (true = Right, false = Left)
/// </summary>
public class BoolToAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOutgoing)
        {
            return isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Grid Column for popup (true = 0 left, false = 1 right)
/// </summary>
public class BoolToPopupColumnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOutgoing)
        {
            return isOutgoing ? 0 : 1; // Outgoing: popup on left (0), Incoming: popup on right (1)
        }
        return 1;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Grid Column for message (true = 1 right, false = 0 left)
/// </summary>
public class BoolToMessageColumnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOutgoing)
        {
            return isOutgoing ? 1 : 0; // Outgoing: message on right (1), Incoming: message on left (0)
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Thickness for popup margin
/// </summary>
public class BoolToPopupMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOutgoing)
        {
            return isOutgoing ? new Thickness(0, 0, 8, 0) : new Thickness(8, 0, 0, 0);
        }
        return new Thickness(8, 0, 0, 0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool (IsSidebarCollapsed) to GridLength for sidebar column
/// </summary>
public class BoolToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed)
        {
            return isCollapsed ? new GridLength(64) : new GridLength(280);
        }
        return new GridLength(280);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string to bool (true if not empty, false if empty)
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Color for message bubbles (outgoing = white, incoming = light blue)
/// Also used for contact selection (selected = light blue, unselected = white)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // For contact selection: true = selected (light blue), false = white
            // For messages: true = outgoing (white), false = incoming (light blue)

            // Check if we're in selection mode (parameter can be used to distinguish)
            if (parameter?.ToString() == "selection")
            {
                return boolValue ? "#B6DBFF" : "#FFFFFF";
            }

            // Default: message bubble mode
            return boolValue ? "#FFFFFF" : "#B6DBFF";
        }
        return "#B6DBFF";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to text color (outgoing = dark, incoming = dark)
/// </summary>
public class BoolToTextColorConverter : IValueConverter
{
    public static readonly BoolToTextColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOutgoing)
        {
            return isOutgoing ? "#364D70" : "#364D70";
        }
        return "#364D70";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool (IsSelected) to background color for contact selection
/// </summary>
public class BoolToSelectionColorConverter : IValueConverter
{
    public static readonly BoolToSelectionColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ? "#B6DBFF" : "#FFFFFF";
        }
        return "#FFFFFF";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}