using Newtonsoft.Json;
using Baconography.OfflineStore;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class GetAdditionalFromListing
    {
        public string BaseURL { get; set; }
        public string After { get; set; }
        public Nullable<int> Limit { get; set; }

        public async Task<Listing> Run(User loggedInUser)
        {
            int limit = Limit ?? 100;
            if (Limit == null && loggedInUser.Me != null && loggedInUser.Me.IsGold)
            {
                limit = 500;
            }

            //means its an offline Listing
            if (After != null && After.StartsWith("#"))
            {
                switch (After[1])
                {
                    case 'l':
                        {
                            //we're looking for a subreddit
                            if(BaseURL.Contains("/r/"))
                            {
                                return await (await Links.GetInstance()).LinksForSubreddit(BaseURL.Substring(BaseURL.IndexOf("/r/") + 3), After);
                            }
                            //we're looking for the main page
                            else
                            {
                                return await (await Links.GetInstance()).AllLinks(After);
                            }
                        }
                    case 'c':
                        {
                            //looking to get more comments
                            return await (await Comments.GetInstance()).GetMoreComments(After, limit);
                        }
                }
            }

            string targetUri = null;
            //if this base url already has arguments (like search) just append the count and the after
            if(BaseURL.Contains(".json?"))
                targetUri = string.Format("{0}&count={1}&after={2}", BaseURL, limit, After);
            else
                targetUri = string.Format("{0}.json?count={1}&after={2}", BaseURL, limit, After);

            try
            {
                var listing = await loggedInUser.SendGet(targetUri);
                var newListing = JsonConvert.DeserializeObject<Listing>(listing);

                if (loggedInUser.AllowOver18)
                    return newListing;
                else
                    return NSFWFilter.RemoveNSFWThings(newListing);
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }
    }
}
