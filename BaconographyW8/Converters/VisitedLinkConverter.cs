using BaconographyPortable.Services;
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
    public class VisitedLinkConverter : IValueConverter
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Goldenrod);
        static SolidColorBrush noHistory = new SolidColorBrush(Colors.DarkOrange);

        IOfflineService _offlineService;
        public VisitedLinkConverter(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (_offlineService.HasHistory(parameter as string))
                return history;
            else
                return noHistory;
                
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
