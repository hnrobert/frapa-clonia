using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

/// <summary>
/// Converts visitor type to a color for the badge
/// </summary>
public class TypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string type)
        {
            return type.ToLowerInvariant() switch
            {
                "stcp" => "#9C27B0",  // Purple
                "xtcp" => "#FF9800",  // Orange
                "sudp" => "#00BCD4",  // Cyan
                _ => "#757575"        // Gray
            };
        }
        return "#757575";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null to false, non-null to true
/// </summary>
public class NotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This converter is one-way only (model to view), so ConvertBack is not implemented
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts empty collections to true, non-empty to false
/// </summary>
public class EmptyCollectionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count == 0;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
