using BaconographyPortable.ViewModel;
using System;
using System.Collections.Generic;
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
    public class VoteColorConverter : IValueConverter
    {
		static SolidColorBrush neutral = new SolidColorBrush(System.Windows.Media.Colors.White);
		static SolidColorBrush upvote = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 103, 00));
		static SolidColorBrush downvote = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 85, 77, 216));

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			if (value is int)
			{
				int val = (int)value;
				if (val == 1)
					return upvote;
				if (val == -1)
					return downvote;
				return neutral;
			}

			return neutral;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
