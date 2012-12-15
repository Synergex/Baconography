using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace BaconographyW8.Converters
{
    public class AlternatingColorConverter : IValueConverter
    {
        static SolidColorBrush even = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
        static SolidColorBrush odd = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var boolVal = (bool)value;
            if (boolVal)
                return even;
            else
                return odd;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
