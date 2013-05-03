using BaconographyPortable.Model.Reddit;
using BaconographyPortable.ViewModel;
using BaconographyWP8.ViewModel;
using Microsoft.Phone.Controls;
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
    public class PageHeadingConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			if (value is SubredditSelectorViewModel)
				return "new";

			if (value is TemporaryRedditViewModel)
			{
				var trvm = value as TemporaryRedditViewModel;
				return "*" + trvm.RedditViewModel.Heading.ToLower();
			}

			if (value is RedditViewModel)
			{
				var rvm = value as RedditViewModel;
				if (rvm.Heading == "The front page of this device")
					return "front page";
				else
					return rvm.Heading.ToLower();
			}

			if (value is TypedThing<Subreddit>)
			{
				var tts = value as TypedThing<Subreddit>;
				return tts.Data.Title;
			}

			return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
