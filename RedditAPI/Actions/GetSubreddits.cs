using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class GetSubreddits
    {
        public Nullable<int> Limit { get; set; }

        public async Task<Listing> Run(User loggedInUser)
        {
            int limit = Limit ?? 100;
            if (Limit == null && loggedInUser.Me.IsGold)
            {
                limit = 1500;
            }

            var modhash = (string)loggedInUser.Me.ModHash;
            var targetUri = string.Format("http://www.reddit.com/reddits/?limit={0}.json", limit);

            try
            {

                var comments = await loggedInUser.SendGet(targetUri);
                var newListing = JsonConvert.DeserializeObject<Listing>(comments);

                if (loggedInUser.AllowOver18)
                    return newListing;
                else
                    return NSFWFilter.RemoveNSFWThings(newListing);
            }
            catch (Exception ex)
            {
                User.ShowDisconnectedMessage();
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }
    }
}
