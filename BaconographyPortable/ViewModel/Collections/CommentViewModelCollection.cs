﻿using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class CommentViewModelCollection : ObservableCollection<ViewModelBase>, IDisposable
    {
        IListingProvider _listingProvider;
        Dictionary<object, object> _state;
        ISystemServices _systemServices;
        IBaconProvider _baconProvider;
        List<WeakReference> _timerHandles;
        string _permaLink;
        string _subreddit;
        string _targetName;

        public CommentViewModelCollection(IBaconProvider baconProvider, string permaLink, string subreddit, string subredditId, string targetName)
        {
            _timerHandles = new List<WeakReference>();
            _state = new Dictionary<object, object>();
            _permaLink = permaLink;
            _subreddit = subreddit;
            _targetName = targetName;
            _baconProvider = baconProvider;
            var settingsService = baconProvider.GetService<ISettingsService>();
            if (settingsService.IsOnline())
                _listingProvider = new BaconographyPortable.Model.Reddit.ListingHelpers.PostComments(baconProvider, subreddit, permaLink, targetName);
            else
                _listingProvider = new BaconographyPortable.Model.KitaroDB.ListingHelpers.PostComments(baconProvider, subredditId, permaLink, targetName);


            //dont add to the observable collection all at once, make the view models on the background thread then start a ui timer to add them 10 at a time
            //to the actual observable collection leaving a bit of time in between so we dont block anything

            _systemServices = baconProvider.GetService<ISystemServices>();
            _systemServices.RunAsync(RunInitialLoad);
            
        }

        async Task RunInitialLoad(object c)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var initialListing = await _listingProvider.GetInitialListing(_state);
            var remainingVMs = MapListing(initialListing, null);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            EventHandler<object> tickHandler = (obj, obj2) => RunUILoad(ref remainingVMs, this, obj);
            _timerHandles.Add(new WeakReference(_systemServices.StartTimer(tickHandler, new TimeSpan(200), true)));
        }

        IEnumerable<ViewModelBase> MapListing(Listing listing, ViewModelBase parent)
        {
            if (listing == null)
                return Enumerable.Empty<ViewModelBase>();
            else
                return listing.Data.Children
                    .Select((thing) => MapThing(thing, parent))
                    .Where(vm => vm != null)
                    .ToList();
        }

        ViewModelBase MapThing(Thing thing, ViewModelBase parent)
        {
            if (thing.Data is More)
            {
                return new MoreViewModel(_baconProvider, ((More)thing.Data).Children, _targetName, _subreddit, RunLoadMore, parent as CommentViewModel);
            }
            else if (thing.Data is Comment)
            {
                var oddNesting = false;
                if(parent is CommentViewModel)
                    oddNesting = !((CommentViewModel)parent).OddNesting;

                var commentViewModel = new CommentViewModel(_baconProvider, thing, ((Comment)thing.Data).LinkId, oddNesting);
                commentViewModel.Replies = new ObservableCollection<ViewModelBase>(MapListing(((Comment)thing.Data).Replies, commentViewModel));
                return commentViewModel;
            }
            else
                return null;
        }

        int CountVMChildren(ViewModelBase vm)
        {
            if (vm is CommentViewModel)
            {
                int counter = 1;
                var commentVM = vm as CommentViewModel;
                foreach(var reply in commentVM.Replies)
                {
                    counter += CountVMChildren(reply);
                }
                return counter;
            }
            else
                return 1;
        }

        void RunUILoad(ref IEnumerable<ViewModelBase> remainingVMs, ObservableCollection<ViewModelBase> targetCollection, object timerHandle)
        {
            _systemServices.StopTimer(timerHandle);
            int vmCount = 0;
            int topLevelVMCount = 0;
            foreach (var vm in remainingVMs)
            {
                topLevelVMCount++;
                vmCount += CountVMChildren(vm);
                targetCollection.Add(vm);

                if (vmCount > 15)
                    break;
            }

            if (vmCount >= 15)
            {
                remainingVMs = remainingVMs.Skip(topLevelVMCount);
                _systemServices.RunAsync(async (obj) =>
                    {
                        _systemServices.RestartTimer(timerHandle);
                    });
            }
        }

        async void RunLoadMore(IEnumerable<string> ids, ObservableCollection<ViewModelBase> targetCollection, ViewModelBase parent)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var initialListing = await _listingProvider.GetMore(ids, _state);
            var remainingVMs = MapListing(initialListing, parent);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            _timerHandles.Add(new WeakReference(_systemServices.StartTimer((obj, obj2) => RunUILoad(ref remainingVMs, targetCollection ?? this, obj), new TimeSpan(200), true)));
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
