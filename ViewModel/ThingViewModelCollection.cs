using GalaSoft.MvvmLight;
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
    public class ThingViewModelCollection : ObservableCollection<ViewModelBase>, ISupportIncrementalLoading
    {
        public IRedditActionQueue ActionQueue { get; set; }
        public Listing TargetListing { get; set; }
        public string BaseListingUrl { get; set; }
        public IUsersService UserService { get; set; }
        public INavigationService NavigationService { get; set; }
        bool _dead = false;

        public bool HasMoreItems
        {
            //have a good url or are currently uninitialized
            get { return !_dead && (TargetListing.Data.After != null || TargetListing.Data.Children.Count == 0); }
        }

        public async Task<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var currentUser = await UserService.GetUser();

            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var getAdditional = new GetAdditionalFromListing { BaseURL = BaseListingUrl, After = TargetListing.Data.After };

            var newListing = await getAdditional.Run(currentUser);

            if (newListing.Data.Children.Count == 0)
                _dead = true;


            foreach (var childThing in newListing.Data.Children)
            {
                if (childThing.Data is Link)
                    Add(new LinkViewModel(childThing, ActionQueue, NavigationService));
                else if (childThing.Data is Comment)
                    Add(new CommentViewModel(childThing, ((Comment)childThing.Data).LinkId, ActionQueue, NavigationService, UserService, true, string.Empty));
                else if (childThing.Data is Subreddit)
                    Add(new SubredditViewModel(childThing, ActionQueue, NavigationService, currentUser, false));
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
}
