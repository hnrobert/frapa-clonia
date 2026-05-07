using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

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
        if (value is true && parameter is string paramValue)
        {
            return paramValue;
        }
        return BindingOperations.DoNothing;
    }
}
