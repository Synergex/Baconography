using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
	/*
	 * Converter that takes a LinkViewModel to determine the type of glyph that should be displayed.
	 * The glyphs are from Segoe UI Symbol which is documented on MSDN: http://msdn.microsoft.com/en-us/library/windows/apps/jj841126.aspx
	 * If an appropriate glyph cannot be determined, a web glyph will be returned
	 */
	public class LinkGlyphConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            return LinkGlyphUtility.GetLinkGlyph(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
    }
}
