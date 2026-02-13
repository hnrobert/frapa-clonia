using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

/// <summary>
/// Converts boolean to "Available" or "Not Available" string
/// </summary>
public class BoolToAvailableStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Available" : "Not Available";
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to "Deployed" or "Not Deployed" string
/// </summary>
public class BoolToDeployedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Deployed" : "Not Deployed";
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts log level string to lowercase for CSS class-style selectors
/// </summary>
public class LevelToLowerConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string level)
        {
            return level.ToLowerInvariant();
        }
        return "info";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string to boolean by comparing with parameter
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string strValue && parameter is string paramValue)
        {
            return string.Equals(strValue, paramValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Only convert back when the value is true (RadioButton being checked)
        // When false, return BindingValue.DoNothing to avoid setting the property
        if (value is bool boolValue && boolValue && parameter is string paramValue)
        {
            return paramValue;
        }
        // Return DoNothing instead of UnsetValue to prevent the crash
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// Negates a boolean value
/// </summary>
public class BoolNegationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}
