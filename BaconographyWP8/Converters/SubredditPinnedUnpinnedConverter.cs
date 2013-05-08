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

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			if (parameter == null)
				return false;

			if (!(parameter is AboutSubredditViewModel))
				return false;

			if (!(value is ObservableCollection<TypedThing<Subreddit>>))
				return false;

			var pinnedSubreddits = value as ObservableCollection<TypedThing<Subreddit>>;
			var subreddit = parameter as AboutSubredditViewModel;

			if (pinnedSubreddits.Contains(subreddit.Thing))
				return true;

			return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
