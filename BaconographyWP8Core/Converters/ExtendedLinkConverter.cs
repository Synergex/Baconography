using BaconographyPortable.ViewModel;
using BaconographyWP8.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class ExtendedLinkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tpl = ((Tuple<bool, LinkViewModel>)value);
            if (tpl.Item1)
            {
                return new ExtendedLinkView { DataContext = tpl.Item2 };
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
