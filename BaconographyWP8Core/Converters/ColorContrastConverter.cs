using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
    public class ColorContrastConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var brush = (SolidColorBrush)value;
            var yiq = ((brush.Color.R * 299) + (brush.Color.G * 587) + (brush.Color.B * 114)) / 1000;
            System.Windows.Media.Color contrastColor;
            bool invert = (parameter != null) && System.Convert.ToBoolean(parameter);

            // check to see if we actually need to invert
            contrastColor = invert
                                ? ((yiq >= 128) ? Colors.White : Colors.Black)
                                : ((yiq >= 128) ? Colors.Black : Colors.White);

            return new SolidColorBrush(contrastColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
