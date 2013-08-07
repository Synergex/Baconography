﻿using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SubredditSubscriptions : IListingProvider
    {
        IUserService _userService;
        IRedditService _redditService;
        IOfflineService _offlineService;
        public SubredditSubscriptions(IBaconProvider baconProvider)
        {
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
        }

        public Tuple<Task<Listing>, Func<Task<Listing>>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Func<Task<Listing>>>(GetCachedListing(), RealUncachedLoad);
        }

        private async Task<Listing> GetCachedListing()
        {
            var things = await _offlineService.RetrieveOrderedThings("sublist:" + (await _userService.GetUser()).Username, TimeSpan.FromDays(1024));
            return new Listing { Data = new ListingData { Children = things != null ? new List<Thing>(things) : new List<Thing>() } };
        }

        private Task<Listing> RealUncachedLoad()
        {
            return UncachedLoad(false);
        }

        private async Task<Listing> UncachedLoad(bool ignoreCache)
        {
            var things = await _offlineService.RetrieveOrderedThings("sublist:" + (await _userService.GetUser()).Username, TimeSpan.FromDays(1));
            if (things != null && !ignoreCache)
            {
                return new Listing { Data = new ListingData { Children = new List<Thing>(things) } };
            }
            else
            {
                Listing resultListing = null;
                var user = await _userService.GetUser();
                if (user != null && user.Me != null)
                {
                    resultListing = await _redditService.GetSubscribedSubredditListing();
                }
                else
                {
                    resultListing = await _redditService.GetDefaultSubreddits();
                }

                await _offlineService.StoreOrderedThings("sublist:" + (await _userService.GetUser()).Username, resultListing.Data.Children);
                return resultListing;
            }
        }

        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return UncachedLoad(true);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits/mine.json", after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
