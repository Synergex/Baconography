using BaconographyPortable.Model.Reddit;
using BaconographyWP8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
    public class AuthorFlairKindConverter : IValueConverter
    {
        SolidColorBrush bg_none = new SolidColorBrush(Colors.Transparent);
        SolidColorBrush bg_mod = new SolidColorBrush(Colors.Red);
        SolidColorBrush bg_op = new SolidColorBrush(Colors.Orange);

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			var kind = (AuthorFlairKind)value;
			switch (kind)
			{
				case AuthorFlairKind.OriginalPoster:
					return bg_op;
				case AuthorFlairKind.Moderator:
					return bg_mod;
				case AuthorFlairKind.None:
					return bg_none;
			}
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
