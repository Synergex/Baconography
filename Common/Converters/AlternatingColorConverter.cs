using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Baconography.Common.Converters
{
    public class AlternatingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            
            var boolVal = (bool)value;
            if (boolVal)
                return new SolidColorBrush(Color.FromArgb(75, 0, 0, 0));
            else
                return new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
