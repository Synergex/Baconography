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
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class CommentViewModelCollection : ObservableCollection<ViewModelBase>, IDisposable
    {
        IListingProvider _listingProvider;
        Dictionary<object, object> _state;
        ISystemServices _systemServices;
        ISettingsService _settingsService;
        IBaconProvider _baconProvider;
        List<WeakReference> _timerHandles;
        string _permaLink;
        string _subreddit;
        string _targetName;
        CancellationTokenSource _cancellationTokenSource;

        public CommentViewModelCollection(IBaconProvider baconProvider, string permaLink, string subreddit, string subredditId, string targetName)
        {
            _timerHandles = new List<WeakReference>();
            _state = new Dictionary<object, object>();
            _permaLink = permaLink;
            _subreddit = subreddit;
            _targetName = targetName;
            _baconProvider = baconProvider;
            _settingsService = baconProvider.GetService<ISettingsService>();
            if (_settingsService.IsOnline())
                _listingProvider = new BaconographyPortable.Model.Reddit.ListingHelpers.PostComments(baconProvider, subreddit, permaLink, targetName);
            
            else
                _listingProvider = new BaconographyPortable.Model.KitaroDB.ListingHelpers.PostComments(baconProvider, subredditId, permaLink, targetName);

            //dont add to the observable collection all at once, make the view models on the background thread then start a ui timer to add them 10 at a time
            //to the actual observable collection leaving a bit of time in between so we dont block anything

            _systemServices = baconProvider.GetService<ISystemServices>();
            _cancellationTokenSource = new CancellationTokenSource();
            RunInitialLoad();
        }

        async void RunInitialLoad()
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            try
            {
                using (_baconProvider.GetService<ISuspendableWorkQueue>().HighValueOperationToken)
                {
                    var initialListing = await _listingProvider.GetInitialListing(_state);
                    var remainingVMs = await MapListing(initialListing, null);
                    RunUILoad(remainingVMs, -1);
                }
            }
            finally
            {
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            }
            
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

                return (await Task.WhenAll(tasks)).Where(vm => vm != null);
            }
                
        }

        async Task<ViewModelBase> MapThing(Thing thing, ViewModelBase parent)
        {
            if (thing.Data is More)
            {
				var depth = 0;
				if (parent is CommentViewModel)
				{
					depth = ((CommentViewModel)parent).Depth + 1;
				}
				var more = new MoreViewModel(_baconProvider, ((More)thing.Data).Children, _targetName, _subreddit, RunLoadMore, parent as CommentViewModel, depth);
				more.Parent = parent as CommentViewModel;
				return more;
            }
            else if (thing.Data is Link)
            {
                var linkView = new LinkViewModel(thing, _baconProvider);
                linkView.FromMultiReddit = true;
                return linkView;
            }
            else if (thing.Data is Comment)
            {
                var oddNesting = false;
                var depth = 0;
                if (parent is CommentViewModel)
                {
                    oddNesting = !((CommentViewModel)parent).OddNesting;
                    depth = ((CommentViewModel)parent).Depth + 1;
                }

                var commentViewModel = new CommentViewModel(_baconProvider, thing, ((Comment)thing.Data).LinkId, oddNesting, depth);
                commentViewModel.Replies = new ObservableCollection<ViewModelBase>(await MapListing(((Comment)thing.Data).Replies, commentViewModel));
                commentViewModel.Parent = parent as CommentViewModel;
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

        void RunBackgroundLoad(ref IEnumerable<ViewModelBase> remainingVMs, int insertionIndex, object timerHandle)
        {
            _systemServices.StopTimer(timerHandle);

            int vmCount = 0;
            int topLevelVMCount = 0;
            foreach (var vm in remainingVMs)
            {
                topLevelVMCount++;
                vmCount += VisitAddChildren(vm, insertionIndex);
                if (vmCount > 15)
                    break;
            }

            if (vmCount >= 15)
            {
                remainingVMs = remainingVMs.Skip(topLevelVMCount);
                _systemServices.RestartTimer(timerHandle);
            }
        }

        void RunUILoad(IEnumerable<ViewModelBase> remainingVMs, int insertionIndex)
        {
            int vmCount = 0;
            int topLevelVMCount = 0;
            foreach (var vm in remainingVMs)
            {
                topLevelVMCount++;
                vmCount += VisitAddChildren(vm, insertionIndex);
                if (vmCount > 15)
                    break;
            }

            if (vmCount >= 15)
            {
                remainingVMs = remainingVMs.Skip(topLevelVMCount);
                EventHandler<object> tickHandler = (obj, obj2) => RunBackgroundLoad(ref remainingVMs, -1, obj);
                _timerHandles.Add(new WeakReference(_systemServices.StartTimer(tickHandler, new TimeSpan(500), true)));
            }
        }


        private int VisitAddChildren(ViewModelBase vm, int index = -1)
        {
            int count = 1;
            if (index < 0)
                this.Add(vm);
            else
                this.Insert(index, vm);

            if (vm is CommentViewModel)
            {
                var comment = vm as CommentViewModel;
                if (comment.Replies != null)
                {
                    foreach (ViewModelBase child in comment.Replies)
                    {
                        count += VisitAddChildren(child, index < 0 ? -1 : index + 1);
                    }
                }
            }
            return count;
        }

        async void RunLoadMore(IEnumerable<string> ids, ObservableCollection<ViewModelBase> targetCollection, ViewModelBase parent, ViewModelBase removeMe)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var initialListing = await _listingProvider.GetMore(ids, _state);

            var remainingVMs = await MapListing(initialListing, parent);
            var insertionIndex = IndexOf(removeMe);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            RunUILoad(remainingVMs, insertionIndex);
            (targetCollection ?? this).Remove(removeMe);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
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
