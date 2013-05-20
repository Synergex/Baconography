using BaconographyPortable.Common;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class BindingShellViewModelCollection : ObservableCollection<ViewModelBase>, PortableISupportIncrementalLoad
    {
        private ThingViewModelCollection DefaultCollection { get; set; }
        private ThingViewModelCollection ActualCollection { get; set; }

        public BindingShellViewModelCollection(ThingViewModelCollection defaultCollection)
        {
            DefaultCollection = defaultCollection;
            ActualCollection = defaultCollection;
            ActualCollection.CollectionChanged += ActualCollection_CollectionChanged;
            foreach (var item in ActualCollection)
            {
                Add(item);
            }
        }

        public void RevertToDefault()
        {
            if (ActualCollection != DefaultCollection)
            {
                ActualCollection.CollectionChanged -= ActualCollection_CollectionChanged;
                ActualCollection = DefaultCollection;
                ActualCollection.CollectionChanged += ActualCollection_CollectionChanged;
                Clear();
                foreach (var item in ActualCollection)
                {
                    Add(item);
                }
            }
        }

        void ActualCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    Remove(oldItem as ViewModelBase);
                }
            }
            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    Add(newItem as ViewModelBase);
                }
            }
        }

        public void UpdateRealItems(ThingViewModelCollection collection)
        {
            ActualCollection.CollectionChanged -= ActualCollection_CollectionChanged;
            ActualCollection = collection;
            ActualCollection.CollectionChanged += ActualCollection_CollectionChanged;

            Clear();
            foreach (var item in ActualCollection)
            {
                Add(item);
            }
            ActualCollection.Refresh();
        }

        public bool HasMoreItems
        {
            get { return ActualCollection.HasMoreItems; }
        }

        public void Refresh()
        {
            ActualCollection.Refresh();
        }

        public Task<int> LoadMoreItemsAsync(uint count)
        {
            return ActualCollection.LoadMoreItemsAsync(count);
        }
    }
}
