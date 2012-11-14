using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public abstract class ThingViewModelCollection : BaseIncrementalLoadCollection<ViewModelBase>
    {
        protected IRedditService _redditService;
        protected INavigationService _navigationService;
        protected IUserService _userService;
        protected IBaconProvider _baconProvider;

        public ThingViewModelCollection(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
        }

        protected override async Task<IEnumerable<ViewModelBase>> InitialLoad(Dictionary<object, object> state)
        {
            state["CurrentUser"] = await _userService.GetUser();
            state["SubscribedSubreddits"] = await _redditService.GetSubscribedSubreddits();
            return MapListing(await GetInitialListing(state), state);
        }

        protected override async Task<IEnumerable<ViewModelBase>> LoadAdditional(Dictionary<object, object> state)
        {
            var after = state["After"] as string;
            state.Remove("After");

            return MapListing(await GetAdditionalListing(after, state), state);
        }

        private IEnumerable<ViewModelBase> MapListing(Listing listing, Dictionary<object, object> state)
        {
            if (listing.Data.After != null)
            {
                state["After"] = listing.Data.After;
            }

            return listing.Data.Children
                .Select(thing => MapThing(thing, state));
        }

        private ViewModelBase MapThing(Thing thing, Dictionary<object, object> state)
        {
            if (thing.Data is Link)
                Add(new LinkViewModel(thing, _baconProvider));
            else if (thing.Data is Comment)
                Add(new CommentViewModel(thing, ((Comment)thing.Data).LinkId, _baconProvider, true, string.Empty));
            else if (thing.Data is Subreddit)
                Add(new AboutSubredditViewModel(_baconProvider, thing, ((HashSet<string>)state["SubscribedSubreddits"]).Contains(((Subreddit)thing.Data).Name)));
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return state.ContainsKey("After");
        }

        protected abstract Task<Listing> GetInitialListing(Dictionary<object, object> state);
        protected abstract Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state);
    }
}
