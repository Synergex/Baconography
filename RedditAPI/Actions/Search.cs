using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class Search
    {
        public string Query { get; set; }
        public int? Limit   { get; set; }

        public async Task<Listing> Run( User loggedInUser )
        {
            int limit = Limit ?? 100;

            if ( loggedInUser.Me != null )
            {
                if ( Limit == null && loggedInUser.Me.IsGold )
                {
                    limit = 1500;
                }
            }

            var targetUri = string.Format( "http://www.reddit.com/search.json?limit={0}&q={1}",
                                           limit,
                                           Query );

            var comments = await loggedInUser.SendGet( targetUri );
            var newListing = JsonConvert.DeserializeObject<Listing>( comments );

            if (loggedInUser.AllowOver18)
                return newListing;
            else
                return NSFWFilter.RemoveNSFWThings(newListing);
        }

        public static string MakeSearchUrl(string term)
        {
            return string.Format("http://www.reddit.com/search.json?q={0}", term);
        }
    }
}
