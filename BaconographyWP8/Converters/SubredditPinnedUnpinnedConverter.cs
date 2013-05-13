using BaconographyPortable.Model.Reddit;
using BaconographyPortable.ViewModel;
using BaconographyWP8.ViewModel;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
	public class SubredditPinnedUnpinnedConverter : IValueConverter
    {
		const string PinGlyph = "\uE141";
		const string UnpinGlyph = "\uE196";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			if (value is bool && (bool)value == true)
				return UnpinGlyph;
			return PinGlyph;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
