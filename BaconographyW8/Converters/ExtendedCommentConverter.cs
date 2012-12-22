using BaconographyPortable.ViewModel;
using BaconographyW8.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace BaconographyW8.Converters
{
    public class ExtendedCommentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var tpl = ((Tuple<bool, CommentViewModel>)value);
            if (tpl.Item1)
            {
                return new ExtendedCommentView { DataContext = tpl.Item2 };
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
