using BaconographyPortable.ViewModel;
using BaconographyWP8.View;
using BaconographyWP8.ViewModel;
using BaconographyWP8.ViewModel.Collections;
using GalaSoft.MvvmLight;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class ReifiedSubredditTemplateCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<PivotItem> boundControls = new ObservableCollection<PivotItem>();
            var redditViewModelCollection = value as RedditViewModelCollection;
            redditViewModelCollection.CollectionChanged += (sender, arg) => redditViewModelCollection_CollectionChanged(sender, arg, boundControls);
            foreach (var viewModel in redditViewModelCollection)
            {
                boundControls.Add(MapViewModel(viewModel));
            }


            if (boundControls.Count > 0 && boundControls[0].Content == null)
            {
                boundControls[0].Content = new RedditView { DataContext = boundControls[0].DataContext };
            }

            return boundControls;
        }

        PivotItem MapViewModel(ViewModelBase viewModel)
        {
            var rvm = viewModel as RedditViewModel;

            var plainHeader = rvm.Heading == "The front page of this device" ? "front page" : rvm.Heading.ToLower();
            return new PivotItem { DataContext = viewModel, Header = rvm.IsTemporary ? "*" + plainHeader : plainHeader };
        }

        void redditViewModelCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e, ObservableCollection<PivotItem> adaptedTarget)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == adaptedTarget.Count)
                    {
                        if (adaptedTarget.Count == 0)
                        {
                            var firstResult = MapViewModel(e.NewItems[0] as ViewModelBase);
                            firstResult.Content = new RedditView { DataContext = e.NewItems[0] };
                            adaptedTarget.Add(firstResult);
                        }
                        else
                            adaptedTarget.Add(MapViewModel(e.NewItems[0] as ViewModelBase));
                    }
                    else
                    {
                        adaptedTarget.Insert(e.NewStartingIndex, MapViewModel(e.NewItems[0] as ViewModelBase));
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    adaptedTarget.RemoveAt(e.OldStartingIndex);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    adaptedTarget[e.OldStartingIndex] = MapViewModel(e.NewItems[0] as ViewModelBase);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    adaptedTarget.Clear();
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
