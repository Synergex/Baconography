using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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

			if (linkViewModel.IsSelfPost)
				return DetailsGlyph;

			var uri = new Uri(linkViewModel.Url);
			var filename = Path.GetFileName(uri.LocalPath);
			var targetHost = uri.DnsSafeHost.ToLower();

			if (linkViewModel.Subreddit == "videos" ||
				targetHost == "www.youtube.com" ||
				targetHost == "youtube.com")
				return VideoGlyph;

			if (targetHost == "www.imgur.com" ||
				targetHost == "imgur.com" ||
				targetHost == "i.imgur.com" ||
				targetHost == "min.us" ||
				targetHost == "www.quickmeme.com" ||
				targetHost == "i.qkme.me" ||
				targetHost == "quickmeme.com" ||
				targetHost == "qkme.me" ||
				targetHost == "memecrunch.com" ||
				targetHost == "flickr.com" ||
				filename.EndsWith(".jpg") ||
				filename.EndsWith(".gif") ||
				filename.EndsWith(".png") ||
				filename.EndsWith(".jpeg"))
				return PhotoGlyph;

			return WebGlyph;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
    }
}
