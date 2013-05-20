using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    public class SubredditInfo : IListingProvider
    {
        IRedditService _redditService;
        IOfflineService _offlineService;
        IUserService _userService;
        public SubredditInfo(IBaconProvider baconProvider)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
            _userService = baconProvider.GetService<IUserService>();
        }

        public Tuple<Task<Listing>, Func<Task<Listing>>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Func<Task<Listing>>>(GetCachedListing(state), () => UncachedLoad(state));
        }

        HashSet<string> HashifyListing(IEnumerable<Thing> listing)
        {
            var hashifyListing = new Func<Thing, string>((thing) =>
            {
                if (thing.Data is Subreddit)
                {
                    return ((Subreddit)thing.Data).Name;
                }
                else
                    return null;
            });

            return new HashSet<string>(listing.Select(hashifyListing)
                    .Where(str => str != null));
        }

		public static Thing GetFrontPageThing()
		{
			Thing frontPage = new Thing();
			frontPage.Data = new Subreddit { DisplayName = "front page", Url = "/", Name = "/", Id="/", Subscribers = 5678123,
											 HeaderImage = "/Assets/reddit.png", PublicDescription = "The front page of this device." };
			frontPage.Kind = "t5";
			return frontPage;
		}

        private async Task<Listing> GetCachedListing(Dictionary<object, object> state)
        {
            state["SubscribedSubreddits"] = HashifyListing(await _offlineService.RetrieveOrderedThings("sublist:" + (await _userService.GetUser()).Username));
            var things = await _offlineService.RetrieveOrderedThings("reddits:");
			if (things.Count() == 0)
				things = new List<Thing>() { GetFrontPageThing() };
            return new Listing { Data = new ListingData { Children = new List<Thing>(things) } };
        }

        private async Task<Listing> UncachedLoad(Dictionary<object, object> state)
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

            if (resultListing != null && resultListing.Data != null && resultListing.Data.Children != null)
            {
                state["SubscribedSubreddits"] = HashifyListing(resultListing.Data.Children);
                await _offlineService.StoreOrderedThings("sublist:" + (await _userService.GetUser()).Username, resultListing.Data.Children);
            }
            else
                state["SubscribedSubreddits"] = new HashSet<string>();

            var subreddits = await _redditService.GetSubreddits(null);
			subreddits.Data.Children.Insert(0, GetFrontPageThing());

            await _offlineService.StoreOrderedThings("reddits:", subreddits.Data.Children.Take(20));
            return subreddits;
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits", after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }


        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return UncachedLoad(state);
        }
    }
}
