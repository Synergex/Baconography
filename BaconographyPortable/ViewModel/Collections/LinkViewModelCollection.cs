using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class LinkViewModelCollection : ThingViewModelCollection
    {
        public LinkViewModelCollection(IBaconProvider baconProvider, string subreddit, string subredditId = null)
            : base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.SubredditLinks(baconProvider, subreddit, subredditId),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.SubredditLinks(baconProvider, subreddit, subredditId)) { }
    }
}
