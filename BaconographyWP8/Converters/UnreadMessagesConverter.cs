using BaconographyPortable.ViewModel;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class UnreadMessagesConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MessageViewModelCollection)
            {
                var collection = (value as MessageViewModelCollection).Where(p => (p as MessageViewModel).IsNew);
                return new ObservableCollection<ViewModelBase>(collection);
            }

            return new ObservableCollection<ViewModelBase>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
