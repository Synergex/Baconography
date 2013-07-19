using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.ViewModel.Collections
{
    public class RedditViewModelCollection : ObservableCollection<ViewModelBase>
    {
        Dictionary<object, object> _state;
        ISystemServices _systemServices;
        IBaconProvider _baconProvider;


		public RedditViewModelCollection(IBaconProvider baconProvider)
        {
        }
    }
}
