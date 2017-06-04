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
            if (values?.Length != 3)
            {
                return false;
            }

            var selectedMod = values[2] as Mod;
            var modFile = values[1] as ModFile;
            var mod = values[0] as Mod;
            if (modFile == null || parameter == null)
            {
                return mod?.Conflicts.Contains(selectedMod) ?? false;
            }

            return modFile.Conflicts.Any(m => m == mod);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("This converter cannot perform reverse conversion.");
        }
    }
}
