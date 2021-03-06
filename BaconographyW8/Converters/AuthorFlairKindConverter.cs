﻿using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace BaconographyW8.Converters
{
    public class AuthorFlairKindConverter : IValueConverter
    {
        SolidColorBrush none = new SolidColorBrush(Colors.Transparent);
        SolidColorBrush mod = new SolidColorBrush(Colors.Blue);
        SolidColorBrush op = new SolidColorBrush(Colors.Orange);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var kind = (AuthorFlairKind)value;

            switch (kind)
            {
                case AuthorFlairKind.OriginalPoster:
                    return op;
                case AuthorFlairKind.Moderator:
                    return mod;
                case AuthorFlairKind.None:
                    return none;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
