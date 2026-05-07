using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

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
        return BindingOperations.DoNothing;
    }
}
