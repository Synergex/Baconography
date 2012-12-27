﻿using BaconographyPortable.Common;
using GalaSoft.MvvmLight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace BaconographyW8.Converters
{
    class PortableAsyncCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new PortableAsyncCollectionWrapper(value as PortableISupportIncrementalLoad ?? new PrebuiltIncrementalLoadCollection<ViewModelBase>(Enumerable.Empty<ViewModelBase>()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        class PortableAsyncCollectionWrapper : ISupportIncrementalLoading, ICollection, IList, INotifyCollectionChanged, INotifyPropertyChanged
        {
            PortableISupportIncrementalLoad _collection;
            public PortableAsyncCollectionWrapper(PortableISupportIncrementalLoad collection)
            {
                _collection = collection;
            }

            public bool HasMoreItems
            {
                get { return _collection.HasMoreItems; }
            }

            public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                return LoadMoreItemsAsyncImpl(count).AsAsyncOperation();
            }

            private async Task<LoadMoreItemsResult> LoadMoreItemsAsyncImpl(uint count)
            {
                return new LoadMoreItemsResult { Count = (uint)await _collection.LoadMoreItemsAsync(count) };
            }

            public void CopyTo(Array array, int index)
            {
                _collection.CopyTo(array, index);
            }

            public int Count
            {
                get { return _collection.Count; }
            }

            public bool IsSynchronized
            {
                get { return _collection.IsSynchronized; }
            }

            public object SyncRoot
            {
                get { return _collection.SyncRoot; }
            }

            public IEnumerator GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged
            {
                add
                {
                    _collection.CollectionChanged += value;
                }
                remove
                {
                    _collection.CollectionChanged -= value;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged
            {
                add
                {
                    _collection.PropertyChanged += value;
                }
                remove
                {
                    _collection.PropertyChanged -= value;
                }
            }

            public int Add(object value)
            {
                return _collection.Add(value);
            }

            public void Clear()
            {
                _collection.Clear();
            }

            public bool Contains(object value)
            {
                return _collection.Contains(value);
            }

            public int IndexOf(object value)
            {
                return _collection.IndexOf(value);
            }

            public void Insert(int index, object value)
            {
                _collection.Insert(index, value);
            }

            public bool IsFixedSize
            {
                get { return _collection.IsFixedSize; }
            }

            public bool IsReadOnly
            {
                get { return _collection.IsReadOnly; }
            }

            public void Remove(object value)
            {
                _collection.Remove(value);
            }

            public void RemoveAt(int index)
            {
                _collection.RemoveAt(index);
            }

            public object this[int index]
            {
                get
                {
                    return _collection[index];
                }
                set
                {
                    _collection[index] = value;
                }
            }
        }
    }
    
}
