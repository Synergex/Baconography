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
                if (tpl.Item2.ExtendedView != null && tpl.Item2.ExtendedView.IsAlive)
                {
                    var existingView = tpl.Item2.ExtendedView.Target as ExtendedLinkView;
                    existingView.DisconnectVM();
                }
                
                var result = new ExtendedLinkView { DataContext = tpl.Item2 };
                tpl.Item2.ExtendedView = new WeakReference(result);
                return result;
                
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
