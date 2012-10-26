using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Baconography.Common.Converters
{
    public class BooleanToZeroBasedIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Nullable<bool>)
            {
                return ((bool)value) ? 0 : 1;
            }
            else
                return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Nullable<int>)
            {
                return ((int)value) == 0 ? true : false;
            }
            else
                return true;
        }
    }
}
