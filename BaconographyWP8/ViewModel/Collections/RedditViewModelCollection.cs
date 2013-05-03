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
    public class RedditViewModelCollection : ObservableCollection<ViewModelBase>, IDisposable
    {
        IListingProvider _listingProvider;
        Dictionary<object, object> _state;
        ISystemServices _systemServices;
        IBaconProvider _baconProvider;
        List<WeakReference> _timerHandles;
        string _permaLink;
        string _subreddit;
        string _targetName;

		public RedditViewModelCollection(IBaconProvider baconProvider)
        {
            _timerHandles = new List<WeakReference>();
            _state = new Dictionary<object, object>();
            _baconProvider = baconProvider;
            var settingsService = baconProvider.GetService<ISettingsService>();
			/*
            if (settingsService.IsOnline())
                _listingProvider = new BaconographyPortable.Model.Reddit.ListingHelpers.PostComments(baconProvider, subreddit, permaLink, targetName);
            else
                _listingProvider = new BaconographyPortable.Model.KitaroDB.ListingHelpers.PostComments(baconProvider, subredditId, permaLink, targetName);
			 */


            //dont add to the observable collection all at once, make the view models on the background thread then start a ui timer to add them 10 at a time
            //to the actual observable collection leaving a bit of time in between so we dont block anything

            _systemServices = baconProvider.GetService<ISystemServices>();
            _systemServices.RunAsync(RunInitialLoad);
            
        }

        async Task RunInitialLoad(object c)
        {
			/*
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var initialListing = await _listingProvider.GetInitialListing(_state);
            var remainingVMs = await MapListing(initialListing, null);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            EventHandler<object> tickHandler = (obj, obj2) => RunUILoad(ref remainingVMs, this, obj);
            _timerHandles.Add(new WeakReference(_systemServices.StartTimer(tickHandler, new TimeSpan(200), true)));
			 */
        }

        public void Dispose()
        {
            foreach (var timer in _timerHandles)
            {
                if (timer.IsAlive)
                {
                    _systemServices.StopTimer(timer.Target);
                }
            }
        }
    }
}
