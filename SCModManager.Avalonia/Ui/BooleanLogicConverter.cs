using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.Avalonia.Ui
{
	public enum LogicOperation
	{
		And,
		Or,
		Xor
	}

	public class BooleanLogicConverter : IMultiValueConverter
	{
		public LogicOperation Operation { get; set; } = LogicOperation.And;

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			var converted = values.Where(v => v is bool).ToArray();

			if (converted.Length == 0)
				return AvaloniaProperty.UnsetValue;

			bool result = (bool)converted[0];
			for (int i = 1; i < converted.Length; i++)
				result = ApplyOperation(result, (bool)converted[i]);

			return result;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}


		private bool ApplyOperation(bool a, bool b)
		{
			switch (Operation)
			{
				case LogicOperation.And: return a && b;
				case LogicOperation.Or: return a || b;
				case LogicOperation.Xor: return a ^ b;
			}

			return false;
		}
	}
}
