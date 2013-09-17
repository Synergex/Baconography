using BaconographyPortable.Model.Reddit;
using BaconographyWP8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
    public class ForegroundAuthorFlairKindConverter : IValueConverter
    {
		static SolidColorBrush fg_none;
		static SolidColorBrush fg_mod;
		static SolidColorBrush fg_op;

		static ForegroundAuthorFlairKindConverter()
		{
			if (Application.Current.Resources.Contains("PhoneAccentBrush"))
				fg_none = Application.Current.Resources["PhoneAccentBrush"] as SolidColorBrush;
			else
				fg_none = Utility.GetColorFromHexa("#FFDAA520");

			if (Application.Current.Resources.Contains("PhoneForegroundBrush"))
				fg_op = Application.Current.Resources["PhoneForegroundBrush"] as SolidColorBrush;
			else
				fg_op = Utility.GetColorFromHexa("#FFDAA520");

			fg_mod = new SolidColorBrush(Colors.Green);
		}

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			var kind = (AuthorFlairKind)value;

			switch (kind)
			{
				case AuthorFlairKind.OriginalPoster:
					return fg_op;
				case AuthorFlairKind.Moderator:
					return fg_mod;
				case AuthorFlairKind.None:
					return fg_none;
			}
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
