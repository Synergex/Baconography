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
    public class DepthColorConverter : IValueConverter
    {
		static SolidColorBrush transparent = new SolidColorBrush(System.Windows.Media.Colors.Transparent);

        static List<SolidColorBrush> depthBrushes = new List<SolidColorBrush>();
        static SolidColorBrush accentBrush = null;

        private void PopulateBrushes()
        {
            var currentAccentBrush = Application.Current.Resources["PhoneAccentBrush"] as SolidColorBrush;
            if (accentBrush != null && currentAccentBrush.Color == accentBrush.Color)
                return;

            depthBrushes.Clear();
            depthBrushes.Add(new SolidColorBrush(System.Windows.Media.Colors.Transparent));
            for (double i = 0.2; i <= 2.0; i+= 0.2)
            {
                int r = (int)(currentAccentBrush.Color.R * i);
                r = r > 255 ? 255 : r;
                int g = (int)(currentAccentBrush.Color.G * i);
                g = g > 255 ? 255 : g;
                int b = (int)(currentAccentBrush.Color.B * i);
                b = b > 255 ? 255 : b;
                depthBrushes.Add(new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)255, (byte)r, (byte)g, (byte)b)));
            }

            accentBrush = currentAccentBrush;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PopulateBrushes();

			int depth = (int)value;
			switch (depth)
			{
				case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                    return depthBrushes[depth];
				default:
                    return depthBrushes[0];
			}
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
