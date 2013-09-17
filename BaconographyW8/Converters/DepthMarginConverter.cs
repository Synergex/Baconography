using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace BaconographyW8.Converters
{
    public class DepthMarginConverter : IValueConverter
    {

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return new Thickness((int)value * 7, 0, 0, 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
