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
    class GetCommentsOnPost
    {
        public string Subreddit { get; set; }
        public string PermaLink { get; set; }
        public Nullable<int> Limit { get; set; }

        public async Task<Listing> Run(User loggedInUser)
        {

            try
            {
                int limit = Limit ?? 500;
                if (Limit == null && loggedInUser.Me != null && loggedInUser.Me.IsGold)
                {
                    limit = 1500;
                }

                Listing listing = null;
                if (PermaLink.StartsWith("#"))
                {
                    var ids = PermaLink.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                    listing = await (await Comments.GetInstance()).GetTopLevelComments(ids[0], ids[1], 25);
                }
                else
                {

                    var targetUri = limit == -1 ?
                        string.Format("http://www.reddit.com{0}.json", PermaLink) :
                        string.Format("http://www.reddit.com{0}.json?limit={1}", PermaLink, limit);

                    var comments = await loggedInUser.SendGet(targetUri);
                    if (comments.StartsWith("["))
                    {
                        var listings = JsonConvert.DeserializeObject<Listing[]>(comments);
                        listing = new Listing { Data = new ListingData { Children = new List<Thing>() } };
                        foreach (var combinableListing in listings)
                        {
                            listing.Data.Children.AddRange(combinableListing.Data.Children);
                            listing.Kind = combinableListing.Kind;
                            listing.Data.After = combinableListing.Data.After;
                            listing.Data.Before = combinableListing.Data.Before;
                        }
                    }
                    else
                        listing = JsonConvert.DeserializeObject<Listing>(comments);
                }

                if (loggedInUser.AllowOver18)
                    return listing;
                else
                    return NSFWFilter.RemoveNSFWThings(listing);
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }
    }
}
