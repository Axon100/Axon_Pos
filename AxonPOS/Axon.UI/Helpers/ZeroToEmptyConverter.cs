using System;
using System.Globalization;
using System.Windows.Data;

namespace Axon.UI.Helpers
{
    /// <summary>
    /// Converts numeric 0 values to empty string for clean TextBox UX across the system.
    /// </summary>
    public class ZeroToEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is decimal d)
                return d == 0m ? string.Empty : d.ToString("0.##", culture);

            if (value is int i)
                return i == 0 ? string.Empty : i.ToString(culture);

            if (value is double db)
                return db == 0 ? string.Empty : db.ToString("0.##", culture);

            if (decimal.TryParse(value.ToString(), out decimal parsed))
                return parsed == 0m ? string.Empty : parsed.ToString("0.##", culture);

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string str = value as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(str))
            {
                if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                    return 0m;
                if (targetType == typeof(int) || targetType == typeof(int?))
                    return 0;
                if (targetType == typeof(double) || targetType == typeof(double?))
                    return 0.0;
                return 0;
            }

            if (decimal.TryParse(str, NumberStyles.Any, culture, out decimal result))
            {
                if (targetType == typeof(int))
                    return (int)result;
                if (targetType == typeof(double))
                    return (double)result;
                return result;
            }

            return 0;
        }
    }
}
