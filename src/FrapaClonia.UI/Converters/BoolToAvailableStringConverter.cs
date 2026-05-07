using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

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
        return BindingOperations.DoNothing;
    }
}
