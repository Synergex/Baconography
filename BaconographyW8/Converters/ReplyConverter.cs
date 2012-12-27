using BaconographyW8.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace BaconographyW8.Converters
{
    class ReplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;
            else
            {
                var control = new ReplyView { DataContext = value };
                return control;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is ReplyView)
            {
                return ((ReplyView)value).DataContext;
            }
            else
                return null;
        }
    }
}
