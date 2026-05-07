using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

public class EmptyStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value?.ToString() ?? "";
        return string.IsNullOrEmpty(str) ? parameter?.ToString() ?? "None" : str;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
