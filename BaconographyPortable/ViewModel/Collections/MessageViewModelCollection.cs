using BaconographyPortable.Messages;
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
    public class MessageViewModelCollection : ObservableCollection<ViewModelBase>, IDisposable
    {
        IListingProvider _listingProvider;
        Dictionary<object, object> _state;
        ISystemServices _systemServices;
        ISettingsService _settingsService;
        IBaconProvider _baconProvider;
        List<WeakReference> _timerHandles;

        public ObservableCollection<ViewModelBase> UnreadMessages { get; private set; }

        public MessageViewModelCollection(IBaconProvider baconProvider)
        {
            _timerHandles = new List<WeakReference>();
            _state = new Dictionary<object, object>();
            _baconProvider = baconProvider;
            _settingsService = baconProvider.GetService<ISettingsService>();
            UnreadMessages = new ObservableCollection<ViewModelBase>();
            //if (_settingsService.IsOnline())
                _listingProvider = new BaconographyPortable.Model.Reddit.ListingHelpers.PostMessages(baconProvider);
            //else
            //    _listingProvider = new BaconographyPortable.Model.KitaroDB.ListingHelpers.PostComments(baconProvider, subredditId, permaLink, targetName);

            //dont add to the observable collection all at once, make the view models on the background thread then start a ui timer to add them 10 at a time
            //to the actual observable collection leaving a bit of time in between so we dont block anything

            _systemServices = baconProvider.GetService<ISystemServices>();
            _systemServices.RunAsync(RunInitialLoad);
            
        }

        async Task RunInitialLoad(object c)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var initialListing = await _listingProvider.GetInitialListing(_state).Item2();
            var remainingVMs = await MapListing(initialListing, null);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            EventHandler<object> tickHandler = (obj, obj2) => RunUILoad(ref remainingVMs, this, obj);
            _timerHandles.Add(new WeakReference(_systemServices.StartTimer(tickHandler, new TimeSpan(200), true)));
        }

        async Task<IEnumerable<ViewModelBase>> MapListing(Listing listing, ViewModelBase parent)
        {
            if (listing == null)
                return Enumerable.Empty<ViewModelBase>();
            else
            {
                var tasks = listing.Data.Children
                    .Select((thing) => Task.Run(() => MapThing(thing, parent)))
                    .ToArray();

                var rval = (await Task.WhenAll(tasks)).Where(vm => vm != null);
                return rval;
            }
                
        }

        async Task<ViewModelBase> MapThing(Thing thing, ViewModelBase parent)
        {
            if (thing.Data is CommentMessage)
            {
                var messageViewModel = new MessageViewModel(_baconProvider, thing);
                messageViewModel.Parent = parent as MessageViewModel;
                return messageViewModel;
            }
            if (thing.Data is Message)
            {
                var oddNesting = false;
                var depth = 0;
                if (parent is MessageViewModel)
                {
                    //oddNesting = !((MessageViewModel)parent).OddNesting;
                    //depth = ((MessageViewModel)parent).Depth + 1;
                }

                var messageViewModel = new MessageViewModel(_baconProvider, thing);
                //commentViewModel.Replies = new ObservableCollection<ViewModelBase>(await MapListing(((Message)thing.Data).Replies, commentViewModel));
                messageViewModel.Parent = parent as MessageViewModel;
                return messageViewModel;
            }
            else
                return null;
        }

        int CountVMChildren(ViewModelBase vm)
        {
            /*
            if (vm is MessageViewModel)
            {
                int counter = 1;
                var messageVM = vm as MessageViewModel;
                foreach (var reply in messageVM.Replies)
                {
                    counter += CountVMChildren(reply);
                }
                return counter;
            }
            else
                return 1;
            */
            return 0;
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
                if (vm is MessageViewModel && (vm as MessageViewModel).IsNew)
                    UnreadMessages.Add(vm);

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

        async void RunLoadMore(IEnumerable<string> ids, ObservableCollection<ViewModelBase> targetCollection, ViewModelBase parent, ViewModelBase removeMe)
        {
            /*
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var initialListing = await _listingProvider.GetMore(ids, _state);

            var remainingVMs = await MapListing(initialListing, parent);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            _timerHandles.Add(new WeakReference(_systemServices.StartTimer((obj, obj2) => 
                {
                    RunUILoad(ref remainingVMs, targetCollection ?? this, obj);
                    (targetCollection ?? this).Remove(removeMe);
                }, new TimeSpan(200), true)));
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
