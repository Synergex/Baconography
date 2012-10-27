using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Baconography.ViewModel
{
    public class SubredditsViewModel : ViewModelBase
    {
        IRedditActionQueue _actionQueue;
        INavigationService _navigationService;
        IUsersService _userService;

        public SubredditsViewModel(IRedditActionQueue actionQueue, INavigationService navigationService, IUsersService userService)
        {
            _actionQueue = actionQueue;
            _navigationService = navigationService;
            _userService = userService;
        }

        public class SubredditViewModelCollection : ObservableCollection<SubredditViewModel>, ISupportIncrementalLoading
        {
            public IRedditActionQueue ActionQueue { get; set; }
            public IUsersService UserService { get; set; }
            public Listing TargetListing { get; set; }
            public User CurrentUser { get; set; }
            public HashSet<string> SubscribedSubreddits { get; set; }
            public string BaseListingUrl { get; set; }
            public INavigationService NavigationService { get; set; }

            public bool HasMoreItems
            {
                //have a good url or are currently uninitialized
                get { return TargetListing.Data.After != null || TargetListing.Data.Children.Count == 0; }
            }

            public async Task<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                if (CurrentUser == null)
                {
                    CurrentUser = await UserService.GetUser();
                    SubscribedSubreddits = await CurrentUser.SubscribedSubreddits();
                }
                if (CurrentUser != null && SubscribedSubreddits == null)
                    SubscribedSubreddits = await CurrentUser.SubscribedSubreddits();

                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                var getAdditional = new GetAdditionalFromListing { BaseURL = BaseListingUrl, After = TargetListing.Data.After };
                var newListing = await getAdditional.Run(CurrentUser);

                foreach (var listing in newListing.Data.Children)
                {
                    Add(new SubredditViewModel(listing, ActionQueue, NavigationService, CurrentUser, SubscribedSubreddits.Contains(((Subreddit)listing.Data).Name)));
                }
                TargetListing = newListing;
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                return new LoadMoreItemsResult { Count = (uint)newListing.Data.Children.Count };
            }

            Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count)
            {
                return AsyncInfo.Run((c) => LoadMoreItemsAsync(count));
            }
        }

        public SubredditViewModel SelectedSubreddit
        {
            get
            {
                return null;
            }
            set
            {
                _navigationService.GoBack();
                _navigationService.Navigate<Baconography.View.RedditView>(new SelectSubreddit { Subreddit = value.Thing });
            }
        }

        SubredditViewModelCollection _subreddits;
        public SubredditViewModelCollection Subreddits
        {
            get
            {
                if (_subreddits == null)
                {
                    _subreddits = new SubredditViewModelCollection
                    {
                        ActionQueue = _actionQueue,
                        NavigationService = _navigationService,
                        UserService = _userService,
                        BaseListingUrl = "http://www.reddit.com/reddits",
                        TargetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                    };
                }
                return _subreddits;
            }
        }
    }
}
