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
    public class ForegroundAuthorFlairKindConverter : IValueConverter
    {
		SolidColorBrush fg_none = Utility.GetColorFromHexa("#FFDAA520");
		SolidColorBrush fg_mod = new SolidColorBrush(Colors.White);
		SolidColorBrush fg_op = new SolidColorBrush(Colors.Black);

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
