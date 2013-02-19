using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class IntegerAdditionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			int original = (int)value;
			int plusval = 1;
			if (parameter != null)
				plusval = (int)parameter;
			return original + plusval;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			int original = (int)value;
			int plusval = 1;
			if (parameter != null)
				plusval = (int)parameter;
			return original - plusval;
        }
    }
}
