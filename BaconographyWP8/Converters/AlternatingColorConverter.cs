using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
    public class AlternatingColorConverter : IValueConverter
    {
        static SolidColorBrush even = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 80, 80, 80));
        static SolidColorBrush odd = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 50, 50, 50));

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var boolVal = (bool)value;
            if (boolVal)
                return even;
            else
                return odd;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
