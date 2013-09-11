using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using System.Windows.Data;
using System.Windows.Media;
using BaconographyWP8Core.Common;

namespace BaconographyWP8.Converters
{
    public class BoolToAccentBrushConverter : IValueConverter
    {
        private static Brush PhoneAccentBrush;
        private static Brush PhoneForegroundBrush;



        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(PhoneAccentBrush == null || PhoneForegroundBrush == null)
            {
                PhoneAccentBrush = Styles.Resources["PhoneAccentBrush"] as Brush;
                PhoneForegroundBrush = Styles.Resources["PhoneForegroundBrush"] as Brush;
            }

            var boolValue = (value as Nullable<bool>) ?? false;
            if (boolValue)
            {
                return PhoneAccentBrush;
            }
            else
            {
                return PhoneForegroundBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
