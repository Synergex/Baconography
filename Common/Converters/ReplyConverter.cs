using Baconography.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Baconography.Common.Converters
{
    class ReplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;
            else
            {
                var control = new ReplyControl { DataContext = value };
                return control;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is ReplyControl)
            {
                return ((ReplyControl)value).DataContext;
            }
            else
                return null;
        }
    }
}
