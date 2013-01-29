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
    public class ExtendedCommentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tpl = ((Tuple<bool, CommentViewModel>)value);
            if (tpl.Item1)
            {
                return new ExtendedCommentView { DataContext = tpl.Item2 };
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
