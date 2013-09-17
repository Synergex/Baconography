using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace BaconographyW8.Converters
{
    public class DepthColorConverter : IValueConverter
    {
		static SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);
        static SolidColorBrush zero = new SolidColorBrush(Colors.Ivory);
		static SolidColorBrush one = new SolidColorBrush(Color.FromArgb(255, 98, 170, 42));
		static SolidColorBrush two = new SolidColorBrush(Color.FromArgb(255, 172, 43, 80));
		static SolidColorBrush three = new SolidColorBrush(Color.FromArgb(255, 191, 84, 48));
        static SolidColorBrush four = new SolidColorBrush(Color.FromArgb(255, 64, 147, 00));
        static SolidColorBrush five = new SolidColorBrush(Color.FromArgb(255, 149, 00, 43));
        static SolidColorBrush six = new SolidColorBrush(Color.FromArgb(255, 166, 42, 00));
        static SolidColorBrush seven = new SolidColorBrush(Color.FromArgb(255, 0, 115, 60));
        static SolidColorBrush eight = new SolidColorBrush(Color.FromArgb(255, 33, 133, 85));
        static SolidColorBrush nine = new SolidColorBrush(Color.FromArgb(255, 255, 150, 64));
        static SolidColorBrush ten = new SolidColorBrush(Color.FromArgb(255, 255, 150, 64));

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			int depth = (int)value;
			switch (depth)
			{
				case 0:
					return zero;
				case 1:
					return one;
				case 2:
					return two;
				case 3:
					return three;
				case 4:
					return four;
				case 5:
					return five;
				case 6:
					return six;
				case 7:
					return seven;
				case 8:
					return eight;
				case 9:
					return nine;
				case 10:
					return ten;

				default:
					return transparent;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
