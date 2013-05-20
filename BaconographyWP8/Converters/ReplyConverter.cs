﻿using BaconographyWP8.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class ReplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            else
            {
				var control = new ReplyViewPage { DataContext = value };
                return control;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			if (value is ReplyViewPage)
            {
				return ((ReplyViewPage)value).DataContext;
            }
            else
                return null;
        }
    }
}
