using System;
using System.Globalization;
using System.Windows.Data;

namespace Candy.Client.Views.Converters
{
    /// <summary>
    /// <see cref="Boolean"/> を反転するコンバーターを提供します。
    /// </summary>
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Invert(value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Invert(value);
        }
        private static object Invert(object value)
        {
            if (value is bool)
            {
                return !((bool)value);
            }
            return false;
        }
    }
}
