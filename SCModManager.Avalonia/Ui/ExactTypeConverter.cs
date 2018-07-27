using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Markup;

namespace SCModManager.Avalonia.Ui
{
    internal class ExactTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType.IsAssignableFrom(value?.GetType()))
            {
                return value;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType?.IsAssignableFrom(value?.GetType()) ?? false)
            {
                return value;
            }

            return null;
        }
    }
}
