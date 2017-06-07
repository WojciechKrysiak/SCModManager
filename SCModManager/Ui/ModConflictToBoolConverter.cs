using SCModManager.ModData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SCModManager.Ui
{
    class ModConflictToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length != 2)
            {
                return false;
            }

            var selectedMod = values[1] as ModConflictSelection;
            var mod = values[0] as ModConflictSelection;
            if (selectedMod != null && mod != null && selectedMod != mod)
            {
                return mod.Files.Any(mf => selectedMod.Files.Any(mff => mff.Path == mf.Path));
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("This converter cannot perform reverse conversion.");
        }
    }
}
