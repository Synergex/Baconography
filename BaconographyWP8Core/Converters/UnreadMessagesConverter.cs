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
                var result = new ObservableCollection<ViewModelBase>(collection);
                (value as MessageViewModelCollection).CollectionChanged += (sender, args) => BridgeChange(result, args);
                return result;
            }

            return new ObservableCollection<ViewModelBase>();
        }

        private void BridgeChange(ObservableCollection<ViewModelBase> target, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if((args.NewItems[0] as MessageViewModel).IsNew)
                        target.Add(args.NewItems[0] as ViewModelBase);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (target.Contains(args.OldItems[0] as ViewModelBase))
                        target.Remove(args.OldItems[0] as ViewModelBase);
                    
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    target.Clear();
                    break;
                default:
                    break;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
