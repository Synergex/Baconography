﻿using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SubredditLinks : IListingProvider
    {
        IRedditService _redditService;
        string _subreddit;
        string _subredditId;

        public SubredditLinks(IBaconProvider baconProvider, string subreddit, string subredditId = null)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _subreddit = subreddit;
            _subredditId = subredditId;
        }

        public Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.GetPostsBySubreddit(_subreddit, null);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://reddit.com" + _subreddit, after, null);
        }

        public async Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            if (string.IsNullOrEmpty(_subredditId))
            {
                var subredditThing = await _redditService.GetSubreddit(_subreddit);
                _subredditId = subredditThing.Data.Name;
            }

            return await _redditService.GetMoreOnListing(ids, _subredditId, _subreddit);
        }
    }
}
