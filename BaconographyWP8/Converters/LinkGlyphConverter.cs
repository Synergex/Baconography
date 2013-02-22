using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
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
	/*
	 * Converter that takes a LinkViewModel to determine the type of glyph that should be displayed.
	 * The glyphs are from Segoe UI Symbol which is documented on MSDN: http://msdn.microsoft.com/en-us/library/windows/apps/jj841126.aspx
	 * If an appropriate glyph cannot be determined, a web glyph will be returned
	 */
	public class LinkGlyphConverter : IValueConverter
    {
		const string NavRightGlyph =	"\uE0AD";
		const string PhotoGlyph =		"\uE114";
		const string VideoGlyph =		"\uE116";
		const string WebGlyph =			"\uE128";
		const string DetailsGlyph =		"\uE14C";

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var linkViewModel = value as LinkViewModel;

			if (linkViewModel == null)
				return WebGlyph;

			if (linkViewModel.Subreddit == "videos")
				return VideoGlyph;

			if (linkViewModel.IsSelfPost)
				return DetailsGlyph;

			if (linkViewModel.HasThumbnail || linkViewModel.HasPreview)
				return PhotoGlyph;

			return WebGlyph;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
    }
}
