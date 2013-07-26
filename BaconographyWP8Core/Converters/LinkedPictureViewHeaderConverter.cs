using BaconographyPortable.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class LinkedPictureViewHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var picture = value as LinkedPictureViewModel.LinkedPicture;
            if (picture != null)
            {
                return string.Format("{0}/{1}", picture.PositionInAlbum, picture.AlbumSize);
            }
            else
                return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
