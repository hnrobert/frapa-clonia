using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace FrapaClonia.UI.Converters;

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
        return BindingOperations.DoNothing;
    }
}
