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
                if (i < 1)
                {
                    int r = currentAccentBrush.Color.R;
                    r = (int)(r == 0 ? 10 * i : r * i);
                    r = r > 255 ? 255 : r;
                    int g = currentAccentBrush.Color.G;
                    g = (int)(g == 0 ? 10 * i : g * i);
                    g = g > 255 ? 255 : g;
                    int b = currentAccentBrush.Color.B;
                    b = (int)(b == 0 ? 10 * i : b * i);
                    b = b > 255 ? 255 : b;
                    depthBrushes.Add(new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)255, (byte)r, (byte)g, (byte)b)));
                }
                else if (i == 1)
                {
                    depthBrushes.Add(currentAccentBrush);
                }
                else
                {
                    double r = (255 - currentAccentBrush.Color.R);
                    r = r == 0 ? 10 : r;
                    r *= (i - 1);
                    r += currentAccentBrush.Color.R;
                    r = r > 255 ? 255 : r;
                    double g = (255 - currentAccentBrush.Color.G);
                    g = g == 0 ? 10 : g;
                    g *= (i - 1);
                    g += currentAccentBrush.Color.G;
                    g = g > 255 ? 255 : g;
                    double b = (255 - currentAccentBrush.Color.B);
                    b = b == 0 ? 10 : b;
                    b *= (i - 1);
                    b += currentAccentBrush.Color.B;
                    b = b > 255 ? 255 : b;
                    depthBrushes.Add(new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)255, (byte)r, (byte)g, (byte)b)));
                }
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
