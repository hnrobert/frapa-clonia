using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

public class TypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string type)
        {
            return type.ToLowerInvariant() switch
            {
                "stcp" => "#9C27B0",
                "xtcp" => "#FF9800",
                "sudp" => "#00BCD4",
                _ => "#757575"
            };
        }
        return "#757575";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
