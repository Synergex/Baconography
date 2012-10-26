using GalaSoft.MvvmLight.Messaging;
using Baconography.Messages;
using Baconography.OfflineStore;
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
    public sealed class CommentViewModelCollection : ObservableCollection<CommentViewModel>, ISupportIncrementalLoading
    {
        string _targetPermalink;
        string _targetId;
        IUsersService _userService;
        IRedditActionQueue _actionQueue;
        INavigationService _nav;
        TypedThing<More> _more;
        string _after;
        string _subreddit;
        bool _oddListing;
        bool _initialLoaded;
        string _opName;
        string _subredditId;

        public CommentViewModelCollection(string subreddit, string targetPermalink, string targetId, string subredditId, IUsersService userService, IRedditActionQueue actionQueue, INavigationService nav, string opName)
        {
            _subreddit = subreddit;
            _targetPermalink = targetPermalink;
            _userService = userService;
            _actionQueue = actionQueue;
            _nav = nav;
            _targetId = targetId;
            _oddListing = true;
            _opName = opName;
            _subredditId = subredditId;
        }

        public CommentViewModelCollection(string subreddit, string targetPermalink, string targetId, string subredditId, IUsersService userService, IRedditActionQueue actionQueue, INavigationService nav, List<Thing> prePopulated, bool oddListing, string opName)
        {
            _opName = opName;
            _subreddit = subreddit;
            _targetPermalink = targetPermalink;
            _userService = userService;
            _actionQueue = actionQueue;
            _nav = nav;
            _targetId = targetId;
            _initialLoaded = true;
            _oddListing = oddListing;
            _subredditId = subredditId;
            foreach (var child in prePopulated)
            {
                //should only be one of these at the end of the collection
                if (child.Data is More)
                {
                    _more = new TypedThing<More>(child);
                }
                else
                {
                    Add(new CommentViewModel(child, _targetId, _actionQueue, _nav, _userService, !oddListing, _opName));
                }
            }
        }

        public bool HasMoreItems
        {
            get 
            {
                return !_initialLoaded || !string.IsNullOrWhiteSpace(_after) || (_more != null && _more.Data.Children.Count > 0);
            }
        }

        private async Task<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            Listing newListing = null;
            if (!_initialLoaded)
            {
                var currentUser = await _userService.GetUser();
                if (!currentUser.IsOnline)
                {
                    var offlinePermalink = string.Format("#{0}#{1}", _subredditId, _targetId);
                    var getComments = new GetCommentsOnPost { PermaLink = offlinePermalink, Subreddit = null, Limit = -1 };
                    newListing = await getComments.Run(currentUser);
                    _after = newListing.Data.After;
                }
                else
                {
                    _initialLoaded = true;
                    var getComments = new GetCommentsOnPost { PermaLink = _targetPermalink, Subreddit = null, Limit = -1 };
                    newListing = await getComments.Run(currentUser);
                    var untypedMore = newListing.Data.Children.FirstOrDefault(thing => thing.Data is More);
                    if (untypedMore != null)
                        _more = new TypedThing<More>(untypedMore);

                    _after = newListing.Data.After;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_after))
                {
                    var moreGetter = new GetMoreOnListing { ChildrenIds = _more.Data.Children.Take(500).ToList(), Subreddit = _subreddit, ContentId = _targetId };
                    newListing = await moreGetter.Run(await _userService.GetUser());
                    var moreMoreComments = newListing.Data.Children.Where(thing => thing.Data is More).ToList();
                    if (moreMoreComments.Count > 0)
                    {
                        var notGottenChildren = moreMoreComments.SelectMany(thing => ((More)thing.Data).Children).ToList();

                        //we asked for more then reddit was willing to give us back
                        //just make sure we dont lose anyone
                        moreGetter.ChildrenIds.RemoveAll((str) => notGottenChildren.Contains(str));
                        //all thats left is what was returned so remove them by value from the moreThing
                        _more.Data.Children.RemoveAll((str) => moreGetter.ChildrenIds.Contains(str));
                    }
                    else
                    {
                        _more.Data.Children.RemoveRange(0, moreGetter.ChildrenIds.Count);
                    }
                }
                else
                {
                    //some kinds of comment requests (such as a user request) will show more using the "after" mechanism instead of "morechildren"
                    var afterGetter = new GetAdditionalFromListing { After = _after, BaseURL = "http://reddit.com" + _targetPermalink };
                    newListing = await afterGetter.Run(await _userService.GetUser());
                    if (newListing != null && newListing.Data != null)
                        _after = newListing.Data.After;
                    else
                        _after = null;
                }
            }

            uint goodChildCount = 0;
            foreach (var child in newListing.Data.Children)
            {
                if (child.Data is Comment)
                {
                    goodChildCount++;
                    Add(new CommentViewModel(child, _targetId, _actionQueue, _nav, _userService, !_oddListing, _opName));
                }
            }
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            return new LoadMoreItemsResult { Count = goodChildCount };
        }

        Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run<LoadMoreItemsResult>((token) => LoadMoreItemsAsync(count));
        }
    }
}
