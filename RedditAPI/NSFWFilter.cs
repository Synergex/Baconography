using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI
{
    static class NSFWFilter
    {
        public static Thing RemoveNSFWThings(Thing thing)
        {
            if (thing.Data is Link || thing.Data is Subreddit)
            {
                if (((dynamic)thing.Data).Over18)
                    return null;
            }

            return thing;
        }

        public static Listing RemoveNSFWThings(Listing listing)
        {
            listing.Data.Children = listing.Data.Children
                .Select(RemoveNSFWThings)
                .Where(thing => thing != null)
                .ToList();

            return listing;
        }
    }
}
