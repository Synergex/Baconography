using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class MarkMessgeRead
    {
        public string Query { get; set; }
        public string Subreddit { get; set; }
        public string Sort { get; set; }
        public Nullable<int> Limit { get; set; }

        public async Task<Listing> Run(User loggedInUser)
        {
            int limit = Limit ?? 100;
            if (Limit == null && loggedInUser.Me.IsGold)
            {
                limit = 1500;
            }


            var modhash = (string)loggedInUser.Me.ModHash;
            var targetUri = string.Format("http://www.reddit.com{0}/search?q={1}&sort={2}&limit={3}.json", 
                string.IsNullOrWhiteSpace(Subreddit) ? "" : "/r/" + Subreddit, Query, Sort, limit);

            var comments = await loggedInUser.SendGet(targetUri);
            return JsonConvert.DeserializeObject<Listing>(comments);
        }
    }
}
