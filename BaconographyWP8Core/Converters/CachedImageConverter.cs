using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BaconographyWP8.Converters
{
    public class CachedImageConverter : IValueConverter
    {
        IImagesService _imagesService;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (_imagesService == null)
                _imagesService = ServiceLocator.Current.GetInstance<IImagesService>();

            if (value is string)
            {
                BitmapSource imageSource = new BitmapImage();
                Action sourceSetter = async () =>
                {
                    var bytes = await _imagesService.ImageBytesFromUrl(value as string);
                    if(bytes != null)
                        imageSource.SetSource(new MemoryStream(bytes));
                };
                sourceSetter();
                return imageSource;

            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
