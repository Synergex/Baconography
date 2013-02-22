using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
    public class VisitedLinkConverter : IValueConverter
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Yellow);
        static SolidColorBrush noHistory = new SolidColorBrush(Colors.Orange);

        IOfflineService _offlineService;
        public VisitedLinkConverter(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (_offlineService.HasHistory(parameter as string))
                return history;
            else
                return noHistory;
                
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisitedMainLinkConverter : IValueConverter
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Gray);
        static Brush noHistory;

        IOfflineService _offlineService;
        public VisitedMainLinkConverter(IBaconProvider baconProvider)
        {
            noHistory = App.Current.Resources["ApplicationForegroundThemeBrush"] as Brush;
            _offlineService = baconProvider.GetService<IOfflineService>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (_offlineService.HasHistory(value as string))
                return history;
            else
                return noHistory;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
