﻿using BaconographyPortable.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Common
{
    public interface PortableISupportIncrementalLoad : ICollection, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        bool HasMoreItems { get; }
        Task<int> LoadMoreItemsAsync(uint count);
    }

    public class PrebuiltIncrementalLoadCollection<T> : ObservableCollection<T>, PortableISupportIncrementalLoad
    {
        public PrebuiltIncrementalLoadCollection(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public bool HasMoreItems
        {
            get { return false; }
        }

        public Task<int> LoadMoreItemsAsync(uint count)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class BaseIncrementalLoadCollection<T> : ObservableCollection<T>, PortableISupportIncrementalLoad
    {
        private bool _initialLoaded;

        //this is to allow a very loose binding of state in the derived classes
        private Dictionary<object, object> _state = new Dictionary<object,object>();

        protected abstract Task<IEnumerable<T>> InitialLoad(Dictionary<object, object> state);
        protected abstract Task<IEnumerable<T>> LoadAdditional(Dictionary<object, object> state);
        protected abstract bool HasAdditional(Dictionary<object, object> state);

        public bool HasMoreItems
        {
            get
            {
                return !_initialLoaded || HasAdditional(_state);
            }
        }

        public async Task<int> LoadMoreItemsAsync(uint count)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });

            int addCounter = 0;

            if (_initialLoaded)
            {
                foreach (var item in await LoadAdditional(_state))
                {
                    addCounter++;
                    Add(item);
                }
            }
            else
            {
                _initialLoaded = true;
                foreach (var item in await InitialLoad(_state))
                {
                    addCounter++;
                    Add(item);
                }
            }

            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            return addCounter;
        }
    }
}
