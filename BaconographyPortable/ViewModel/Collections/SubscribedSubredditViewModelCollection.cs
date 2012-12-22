using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class SubscribedSubredditViewModelCollection : ThingViewModelCollection
    {
        public SubscribedSubredditViewModelCollection(IBaconProvider baconProvider)
            : base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.SubredditSubscriptions(baconProvider),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.SubredditSubscriptions(baconProvider)) { }
    }
}
