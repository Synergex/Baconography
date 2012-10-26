using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class GetMoreOnListing
    {
        public List<string> ChildrenIds { get; set; }
        public string ContentId {get; set;}
        public string Subreddit {get; set;}

        public async Task<Listing> Run(User loggedInUser)
        {
            var targetUri = "http://www.reddit.com/api/morechildren.json";

            var arguments = new Dictionary<string, string>
            {
                {"children", string.Join(",", ChildrenIds) },
                {"link_id", ContentId },
                {"pv_hex", ""},
                {"api_type", "json" }
            };

            if (Subreddit != null)
            {
                arguments.Add("r", Subreddit);
            }

            try
            {
                var result = await loggedInUser.SendPost(new FormUrlEncodedContent(arguments), targetUri);
                var newListing = new Listing
                {
                    Kind = "Listing",
                    Data = new ListingData { Children = JsonConvert.DeserializeObject<JsonThing>(result).Json.Data.Things }
                };

                if (loggedInUser.AllowOver18)
                    return newListing;
                else
                    return NSFWFilter.RemoveNSFWThings(newListing);
            }
            catch
            {
                User.ShowDisconnectedMessage();
                
                return new Listing
                {
                    Kind = "Listing",
                    Data = new ListingData { Children = new List<Thing>() }
                };
            }
            
        }
    }
}
