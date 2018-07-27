using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SCModManager.Avalonia.Ui
{
    public class BoolToValueConverter<T> : IValueConverter
    {
		public T TrueValue { get; set; }

		public T FalseValue { get; set; }

		public T UnsetValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
				return (bool)value ? TrueValue : FalseValue;

			return UnsetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
