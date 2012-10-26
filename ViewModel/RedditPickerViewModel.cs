using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
    public class RedditPickerViewModel : ViewModelBase
    {
        INavigationService _nav;
        IUsersService _userService;
        public RedditPickerViewModel(INavigationService nav, IUsersService userService)
        {
            _nav = nav;
            _userService = userService;

            _subreddits = new SubscribedSubredditsCollection
            {
                BaseListing = GetSubs,
                UserService = _userService
            };

            MessengerInstance.Register<UserLoggedIn>(this, (userMessage) =>
                {
                    if (userMessage.CurrentUser != null && userMessage.CurrentUser.Me != null)
                    {
                        var subscribedSubredditGetter = new GetSubscribedSubreddits();
                        Subreddits = new SubscribedSubredditsCollection { BaseListing = GetSubs, UserService = _userService };
                    }
                });
        }

        private async Task<Listing> GetSubs(User user)
        {
            if (user == null || user.Me == null)
            {
                return await GetSubscribedSubreddits.Defaults();
            }
            else
            {
                var getter = new GetSubscribedSubreddits();
                return await getter.Run(user);
            }
        }

        RelayCommand _showViewAll;
        public RelayCommand ShowViewAll
        {
            get
            {
                if (_showViewAll == null)
                {
                    _showViewAll = new RelayCommand(() =>
                        {
                            _nav.Navigate<Baconography.View.SubredditsView>(null);
                        });
                }
                return _showViewAll;
            }
        }

        RelayCommand _showMultiReddit;
        public RelayCommand ShowMultiReddit
        {
            get
            {
                if (_showMultiReddit == null)
                {
                    _showMultiReddit = new RelayCommand(() =>
                        {
                        });
                }
                return _showMultiReddit;
            }
        }

        public Subreddit SelectedSubreddit
        {
            get
            {
                return null;
            }
            set
            {
                _nav.Navigate<Baconography.View.RedditView>(new SelectSubreddit { Subreddit = new RedditAPI.TypedThing<Subreddit>(new Thing { Kind = "t5", Data = value }) });
            }
        }

        public class SubscribedSubredditsCollection : ObservableCollection<Subreddit>, ISupportIncrementalLoading
        {
            public Func<User, Task<Listing>> BaseListing { get; set; }
            public IUsersService UserService { get; set; }
            private bool _initialized = false;

            public bool HasMoreItems
            {
                //have a good url or are currently uninitialized
                get { return !_initialized; }
            }

            public async Task<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                _initialized = true;
                var currentUser = await UserService.GetUser();
                var newListing = await BaseListing(currentUser);

                foreach (var child in newListing.Data.Children)
                {
                    if (child.Data is Subreddit)
                        Add(child.Data as Subreddit);
                }

                return new LoadMoreItemsResult { Count = (uint)newListing.Data.Children.Count };
            }

            Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count)
            {
                return AsyncInfo.Run((c) => LoadMoreItemsAsync(count));
            }
        }



        SubscribedSubredditsCollection _subreddits;
        public SubscribedSubredditsCollection Subreddits
        {
            get
            {
                return _subreddits;
            }
            set
            {
                _subreddits = value;
                RaisePropertyChanged("Subreddits");
            }
        }
    }
}
