using System;
using System.Globalization;
using System.Windows.Data;

namespace Axon.UI.Helpers
{
    public class StringEqualsToStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return System.Windows.Application.Current.FindResource("PeriodBtnInactive");

            string currentVal = value.ToString() ?? "";
            string targetVal = parameter.ToString() ?? "";

            if (currentVal.Equals(targetVal, StringComparison.OrdinalIgnoreCase))
            {
                return System.Windows.Application.Current.FindResource("PeriodBtnActive");
            }

            return System.Windows.Application.Current.FindResource("PeriodBtnInactive");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
